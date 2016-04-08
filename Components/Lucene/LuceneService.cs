using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;
using DotNetNuke.Common;
using Lucene.Net.Analysis;
using Satrabel.OpenFiles.Components.Lucene.Mapping;
using Directory = Lucene.Net.Store.Directory;

namespace Satrabel.OpenFiles.Components.Lucene
{
    public class LuceneService : IDisposable
    {

        #region Constants
        private const string WriteLockFile = "write.lock";
        private const int DefaultRereadTimeSpan = 10; // in seconds (initialy 30sec)
        private const int DISPOSED = 1;
        private const int UNDISPOSED = 0;
        #endregion

        #region Private Properties

        private readonly string _searchFolder;
        private readonly Analyzer _analyser;

        private string IndexFolder { get; set; }

        private IndexWriter _writer;
        private IndexReader _idxReader;
        private CachedReader _reader;
        private readonly object _writerLock = new object();
        private readonly double _readerTimeSpan; // in seconds
        private readonly List<CachedReader> _oldReaders = new List<CachedReader>();
        private int _isDisposed = UNDISPOSED;

        private static FSDirectory _directoryTemp;

        #region constructor
        internal LuceneService(string searchFolder, Analyzer analyser)
        {
            _searchFolder = searchFolder;
            _analyser = analyser;
            if (string.IsNullOrEmpty(_searchFolder))
                throw new ArgumentNullException("searchFolder");
            IndexFolder = Path.Combine(Globals.ApplicationMapPath, _searchFolder);
            _readerTimeSpan = DefaultRereadTimeSpan; //  hostController.GetDouble(Constants.SearchReaderRefreshTimeKey, DefaultRereadTimeSpan);
        }

        private void CheckDisposed()
        {
            if (Thread.VolatileRead(ref _isDisposed) == DISPOSED)
                throw new ObjectDisposedException(string.Format("LuceneController [{0}] is disposed and cannot be used anymore", _searchFolder));
        }
        #endregion


        private static IndexWriter MyWriter(Directory outputFolder, Analyzer analyzer, bool allowCreate)
        {
            return new IndexWriter(outputFolder, analyzer, allowCreate, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        private IndexWriter Writer
        {
            get
            {
                if (_writer == null)
                {
                    lock (_writerLock)
                    {
                        if (_writer == null)
                        {
                            var lockFile = Path.Combine(IndexFolder, WriteLockFile);
                            if (File.Exists(lockFile))
                            {
                                try
                                {
                                    // if we successd in deleting the file, move on and create a new writer; otherwise,
                                    // the writer is locked by another instance (e.g., another server in a webfarm).
                                    File.Delete(lockFile);
                                }
                                catch (IOException e)
                                {
#pragma warning disable 0618
                                    throw new Exception("Unable to create Lucene writer (lock file is in use). Please recycle AppPool in IIS to release lock.", e);
#pragma warning restore 0618
                                }
                            }

                            CheckDisposed();
                            var writer = new IndexWriter(FSDirectory.Open(IndexFolder), _analyser, IndexWriter.MaxFieldLength.UNLIMITED);
                            _idxReader = writer.GetReader();
                            Thread.MemoryBarrier();
                            _writer = writer;
                        }
                    }
                }
                return _writer;
            }
        }

        // made internal to be used in unit tests only; otherwise could be made private
        internal IndexSearcher GetSearcher()
        {
            if (_reader == null || MustRereadIndex)
            {
                CheckValidIndexFolder();
                UpdateLastAccessTimes();
                InstantiateReader();
            }

            return _reader.GetSearcher();
        }

        private void InstantiateReader()
        {
            IndexSearcher searcher;
            if (_idxReader != null)
            {
                //use the Reopen() method for better near-realtime when the _writer ins't null
                var newReader = _idxReader.Reopen();
                if (_idxReader != newReader)
                {
                    //_idxReader.Dispose(); -- will get disposed upon disposing the searcher
                    Interlocked.Exchange(ref _idxReader, newReader);
                }

                searcher = new IndexSearcher(_idxReader);
            }
            else
            {
                // Note: disposing the IndexSearcher instance obtained from the next
                // statement will not close the underlying reader on dispose.
                searcher = new IndexSearcher(FSDirectory.Open(IndexFolder));
            }

            var reader = new CachedReader(searcher);
            var cutoffTime = DateTime.Now - TimeSpan.FromSeconds(_readerTimeSpan * 10);
            lock (((ICollection)_oldReaders).SyncRoot)
            {
                CheckDisposed();
                _oldReaders.RemoveAll(r => r.LastUsed <= cutoffTime);
                _oldReaders.Add(reader);
                Interlocked.Exchange(ref _reader, reader);
            }
        }

        private DateTime _lastReadTimeUtc;
        private DateTime _lastDirModifyTimeUtc;

        private bool MustRereadIndex
        {
            get
            {
                return (DateTime.UtcNow - _lastReadTimeUtc).TotalSeconds >= _readerTimeSpan &&
                    System.IO.Directory.Exists(IndexFolder) &&
                    System.IO.Directory.GetLastWriteTimeUtc(IndexFolder) != _lastDirModifyTimeUtc;
            }
        }

        private void UpdateLastAccessTimes()
        {
            _lastReadTimeUtc = DateTime.UtcNow;
            if (System.IO.Directory.Exists(IndexFolder))
            {
                _lastDirModifyTimeUtc = System.IO.Directory.GetLastWriteTimeUtc(IndexFolder);
            }
        }

        private void RescheduleAccessTimes()
        {
            // forces re-opening the reader within 30 seconds from now (used mainly by commit)
            var now = DateTime.UtcNow;
            if (_readerTimeSpan > DefaultRereadTimeSpan && (now - _lastReadTimeUtc).TotalSeconds > DefaultRereadTimeSpan)
            {
                _lastReadTimeUtc = now - TimeSpan.FromSeconds(_readerTimeSpan - DefaultRereadTimeSpan);
            }
        }

        private void CheckValidIndexFolder()
        {
            if (!ValidateIndexFolder())
                throw new Exception(string.Format("OpenFiles Search indexing directory [{0}] is either empty or does not exist", _searchFolder));
        }

        private static FSDirectory LuceneOutputFolder
        {
            get
            {
                string luceneOutputPath = Path.Combine(Globals.ApplicationMapPath, "App_Data\\OpenFiles\\LuceneIndex");

                if (_directoryTemp != null && !System.IO.Directory.Exists(luceneOutputPath))
                {
                    Log.Logger.DebugFormat("Lucene index directory [{0}] seems to have been deleted. Resetting.", luceneOutputPath);
                    _directoryTemp = null;
                }
                if (_directoryTemp == null)
                    _directoryTemp = FSDirectory.Open(new DirectoryInfo(luceneOutputPath));
                if (IndexWriter.IsLocked(_directoryTemp))
                    IndexWriter.Unlock(_directoryTemp);
                var lockFilePath = Path.Combine(luceneOutputPath, WriteLockFile);
                if (File.Exists(lockFilePath))
                    File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }

        public void Initialise(Action reindexer)
        {
            if (ValidateIndexFolder())
                return;

            Log.Logger.DebugFormat("Lucene index directory [{0}] being initialized.", LuceneOutputFolder);
            reindexer.Invoke();
            Log.Logger.DebugFormat("Lucene index directory [{0}] finished initializing.", LuceneOutputFolder);
        }

        internal bool ValidateIndexFolder()
        {
            return System.IO.Directory.Exists(IndexFolder) &&
                   System.IO.Directory.GetFiles(IndexFolder, "*.*").Length > 0;
        }

        #endregion


        // search methods
        internal List<LuceneIndexItem> GetAllIndexedRecords()
        {
            Log.Logger.DebugFormat("Executing ==> internal static List<LuceneIndexItem> GetAllIndexedRecords()");
            // validate search index
            if (!ValidateIndexFolder())
                return new List<LuceneIndexItem>();

            // set up lucene searcher
            var searcher = new IndexSearcher(LuceneOutputFolder, false);
            var reader = IndexReader.Open(LuceneOutputFolder, false);
            var docs = new List<Document>();
            var term = reader.TermDocs();
            // v 2.9.4: use 'term.Doc()'
            // v 3.0.3: use 'term.Doc'
            while (term.Next()) docs.Add(searcher.Doc(term.Doc));
            reader.Dispose();
            searcher.Dispose();
            return MapLuceneToDataList(docs);
        }

        #region Operations

        public void Add(Document doc)
        {
            Requires.NotNull("searchDocument", doc);
            if (doc.GetFields().Count > 0)
            {
                try
                {
                    Writer.AddDocument(doc);
                }
                catch (OutOfMemoryException)
                {
                    lock (_writerLock)
                    {
                        // as suggested by Lucene's doc
                        DisposeWriter();
                        Writer.AddDocument(doc);
                    }
                }
            }
        }

        public void Delete(Query query)
        {
            Requires.NotNull("luceneQuery", query);
            Writer.DeleteDocuments(query);
        }

        public void DeleteOld(int indexId)
        {
            if (ValidateIndexFolder())
            {
                // init lucene
                var analyzer = DnnFilesMappingUtils.GetAnalyser();
                using (var writer = new IndexWriter(LuceneOutputFolder, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // remove older index entry
                    var searchQuery = new TermQuery(new Term(DnnFilesMappingUtils.GetIndexFieldName(), indexId.ToString()));
                    writer.DeleteDocuments(searchQuery);

                    // close handles
                    analyzer.Close();
                    writer.Dispose();
                }
            }
            else
            {
                Log.Logger.DebugFormat("Failed to remove record from {0} Lucene index. Index does not exist. ", indexId);
            }
        }

        public void Commit()
        {
            if (_writer != null)
            {
                lock (_writerLock)
                {
                    if (_writer != null)
                    {
                        CheckDisposed();
                        _writer.Commit();
                        RescheduleAccessTimes();
                    }
                }
            }
        }

        internal void AddOld(LuceneIndexItem item)
        {
            AddOld(new List<LuceneIndexItem> { item });
        }

        internal void AddOld(List<LuceneIndexItem> itemlist)
        {
            if (!itemlist.Any()) return;

            var analyzer = DnnFilesMappingUtils.GetAnalyser();
            using (var writer = MyWriter(LuceneOutputFolder, analyzer, !ValidateIndexFolder()))
            {
                // add data to lucene search index (replaces older entries if any)
                foreach (var file in itemlist)
                {
                    Log.Logger.DebugFormat("Indexing file {0}/{1}.", file.Folder, file.FileName);
                    AddToLuceneIndex(file, writer);
                }

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
            var counter = 0;
            while (!ValidateIndexFolder() && counter < 20)
            {
                counter += 1;
                Log.Logger.DebugFormat("checking IndexExists {0}", counter);
                Thread.Sleep(200); //give lucene some time to write everything to disk
            }
        }

        private static void AddToLuceneIndex(LuceneIndexItem item, IndexWriter writer)
        {
            // remove older index entry
            var searchQuery = new TermQuery(new Term(DnnFilesMappingUtils.GetIndexFieldName(), GetIndexFieldValue(item)));
            writer.DeleteDocuments(searchQuery);

            // add new index entry
            var luceneDoc = new Document();

            // add lucene fields mapped to db fields
            luceneDoc.Add(new Field("PortalId", item.PortalId.ToString(), Field.Store.NO, Field.Index.ANALYZED));
            luceneDoc.Add(new Field("FileId", item.FileId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field("FileName", item.FileName, Field.Store.NO, Field.Index.ANALYZED));
            luceneDoc.Add(new Field("Folder", item.Folder, Field.Store.NO, Field.Index.ANALYZED));
            if (!string.IsNullOrEmpty(item.Title))
                luceneDoc.Add(new Field("Title", item.Title, Field.Store.NO, Field.Index.ANALYZED));
            if (!string.IsNullOrEmpty(item.Description))
                luceneDoc.Add(new Field("Description", item.Description, Field.Store.NO, Field.Index.ANALYZED));
            if (!string.IsNullOrEmpty(item.FileContent))
                luceneDoc.Add(new Field("FileContent", item.FileContent, Field.Store.NO, Field.Index.ANALYZED));

            if (item.Categories != null)
            {
                foreach (var cat in item.Categories)
                {
                    luceneDoc.Add(new Field("Category", cat, Field.Store.NO, Field.Index.ANALYZED));
                }
            }
            // add entry to index
            try
            {
                writer.AddDocument(luceneDoc);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(string.Format("Failed to index File [{0}:{1}]", item.FileId, item.Title), ex);
            }
        }

        public bool DeleteAll()
        {
            var retval = true;
            Log.Logger.DebugFormat("Executing ==> public static bool ClearLuceneIndex()");
            try
            {
                if (ValidateIndexFolder())
                {
                    var analyzer = DnnFilesMappingUtils.GetAnalyser();
                    using (var writer = MyWriter(LuceneOutputFolder, analyzer, false))
                    {
                        Log.Logger.DebugFormat("          ==> Deleting all documents from index");
                        // remove older index entries
                        writer.DeleteAll();

                        // close handles
                        analyzer.Close();
                        writer.Dispose();
                    }
                }
                else
                {
                    Log.Logger.DebugFormat("          ==> Nothing to delete: Index is missing!!");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.DebugFormat("          ==> Deleting documents failed with {0}", ex.Message);
                retval = false;
            }
            Log.Logger.DebugFormat("     Exit ==> public static bool ClearLuceneIndex() with {0}", retval);
            return retval;
        }
        public bool OptimizeSearchIndex(bool doWait)
        {
            var writer = _writer;
            if (writer != null && writer.HasDeletions())
            {
                if (doWait)
                {
                    OpenContent.Components.Log.Logger.Debug("Compacting Search Index - started");
                }

                CheckDisposed();
                //optimize down to "> 1 segments" for better performance than down to 1
                _writer.Optimize(4, doWait);

                if (doWait)
                {
                    Commit();
                    OpenContent.Components.Log.Logger.Debug("Compacting Search Index - finished");
                }

                return true;
            }

            return false;
        }

        public void OptimizeOld()
        {
            if (!ValidateIndexFolder())
            {
                Log.Logger.DebugFormat("Lucene index [{0}] can not be optimized as it does not exist.", LuceneOutputFolder.Directory.Name);
                return;
            }
            var analyzer = DnnFilesMappingUtils.GetAnalyser();
            using (var writer = MyWriter(LuceneOutputFolder, analyzer, false))
            {
                analyzer.Close();
                writer.Optimize();
                writer.Dispose();
            }
        }

        #endregion

        #region Private Methods

        internal List<LuceneIndexItem> Search(string searchQuery, string searchField = "")
        {
            var fieldlist = DnnFilesMappingUtils.GetSearchAllFieldList(); //todo param
            // main search method

            // validation
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<LuceneIndexItem>();
            //searchQuery = searchQuery.Replace("*", "").Replace("?", "");  //don't allow wilcard?! Well, for finding folders, wildcards are not allowed (Demetris)

            // set up lucene searcher
            using (var searcher = new IndexSearcher(LuceneOutputFolder, true))
            {
                ScoreDoc[] hits;
                const int hitsLimit = 1000;
                var analyzer = DnnFilesMappingUtils.GetAnalyser();

                // search by single field
                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(Version.LUCENE_30, searchField, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    Log.Logger.DebugFormat("Querying 1 Lucene Index with: [{0}]", query.ToString());
                    hits = searcher.Search(query, hitsLimit).ScoreDocs;
                }
                // search by multiple fields (ordered by INDEXORDER)
                else
                {
                    var parser = new MultiFieldQueryParser(Version.LUCENE_30, fieldlist, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    Log.Logger.DebugFormat("Querying 2 Lucene Index with: [{0}]", query.ToString());
                    hits = searcher.Search(query, null, hitsLimit, Sort.INDEXORDER).ScoreDocs;
                }
                Log.Logger.DebugFormat("Querying resulted in [{0}] hits from {1}", hits.Length, LuceneOutputFolder.Directory.FullName);
                var results = MapLuceneToDataList(hits, searcher);
                analyzer.Close();
                searcher.Dispose();
                return results;
            }
        }

        private static Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        private static List<LuceneIndexItem> MapLuceneToDataList(IEnumerable<Document> hits)
        {
            // map Lucene search index to data
            return hits.Select(DnnFilesMappingUtils.MapLuceneDocumentToData).ToList(); //todo param
        }

        private static List<LuceneIndexItem> MapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            // v 2.9.4: use 'hit.doc'
            // v 3.0.3: use 'hit.Doc'
            return hits.Select(hit => DnnFilesMappingUtils.MapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList(); //todo param
        }

        private static string GetIndexFieldValue(LuceneIndexItem item)
        {
            return item.FileId.ToString();
        }

        private static class LockKeys
        {
            public static string IndexWriterLockKey(string file)
            {
                return String.Format("IndexWriter_{0}", file);
            }
        }

        #endregion

        public void Dispose()
        {
            var status = Interlocked.CompareExchange(ref _isDisposed, DISPOSED, UNDISPOSED);
            if (status == UNDISPOSED)
            {
                DisposeWriter();
                DisposeReaders();
            }
        }

        private void DisposeWriter()
        {
            if (_writer != null)
            {
                lock (_writerLock)
                {
                    if (_writer != null)
                    {
                        _idxReader.Dispose();
                        _idxReader = null;

                        _writer.Commit();
                        _writer.Dispose();
                        _writer = null;
                    }
                }
            }
        }

        private void DisposeReaders()
        {
            lock (((ICollection)_oldReaders).SyncRoot)
            {
                foreach (var rdr in _oldReaders)
                {
                    rdr.Dispose();
                }
                _oldReaders.Clear();
                _reader = null;
            }
        }

        class CachedReader : IDisposable
        {
            public DateTime LastUsed { get; private set; }
            private readonly IndexSearcher _searcher;

            public CachedReader(IndexSearcher searcher)
            {
                _searcher = searcher;
                UpdateLastUsed();
            }

            public IndexSearcher GetSearcher()
            {
                UpdateLastUsed();
                return _searcher;
            }

            private void UpdateLastUsed()
            {
                LastUsed = DateTime.Now;
            }

            public void Dispose()
            {
                _searcher.Dispose();
                _searcher.IndexReader.Dispose();
            }
        }
    }
}