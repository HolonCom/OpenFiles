#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Satrabel.OpenContent.Components.Lucene.Mapping;
using Satrabel.OpenContent.Components.Lucene.Config;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Search.Internals;
using Satrabel.OpenFiles.Components.Lucene.Mapping;
using Version = Lucene.Net.Util.Version;

#endregion

namespace Satrabel.OpenFiles.Components.Lucene
{
    public class LuceneController : IDisposable
    {
        private static LuceneController _instance = new LuceneController();
        private LuceneService _serviceInstance;
		
        public static LuceneController Instance
        {
            get
            {
                return _instance;
            }
        }
        public LuceneService Store
        {
            get
            {
                if (_serviceInstance == null)
                    throw new Exception("LuceneController not initialized properly");
                return _serviceInstance;
            }
        }
        #region constructor
        private LuceneController()
        {
            _serviceInstance = new LuceneService(@"App_Data\OpenFiles\LuceneIndex", DnnFilesMappingUtils.GetAnalyser());

        }
        public static void ClearInstance()
        {

            _instance.Dispose();
            _instance = null;
            _instance = new LuceneController();
        }
        #endregion

        #region Search

        public SearchResults Search(string input, string fieldName = "")
        {
            Log.Logger.DebugFormat("Executing ==> public static IEnumerable<LuceneIndexItem> Search(string [{0}], string [{1}])", input, fieldName);
            SearchResults retval;
            ValidateIndex();
            if (string.IsNullOrEmpty(input))
                retval = new SearchResults(Store.GetAllIndexedRecords());
            else
                retval = new SearchResults(Store.Search(input, fieldName));
            retval.TotalResults = retval.ids.Count;
            Log.Logger.DebugFormat("     Exit ==> public static IEnumerable<LuceneIndexItem> Search() with {0} items found", retval.TotalResults);
            return retval;
        }
        //private SearchResults Search(string type, Query filter, Query query, Sort sort, int pageSize, int pageIndex)
        //{

        //    //validate whether index folder is exist and contains index files, otherwise return null.
        //    ValidateIndex();

        //    var searcher = Store.GetSearcher();
        //    TopDocs topDocs;
        //    if (filter == null)
        //        topDocs = searcher.Search(type, query, (pageIndex + 1) * pageSize, sort);
        //    else
        //        topDocs = searcher.Search(type, filter, query, (pageIndex + 1) * pageSize, sort);
        //    var luceneResults = new SearchResults(topDocs.ScoreDocs.Skip(pageIndex * pageSize).Select(d => searcher.Doc(d.Doc).GetField(DnnFilesMappingUtils.FieldId).StringValue).ToArray(), topDocs.TotalHits);
        //    luceneResults.TotalResults = topDocs.TotalHits;
        //    luceneResults.ids = topDocs.ScoreDocs.Skip(pageIndex * pageSize).Select(d => searcher.Doc(d.Doc).GetField(DnnFilesMappingUtils.FieldId).StringValue).ToArray();
        //    return luceneResults;
        //}

        public SearchResults GetAllIndexedRecords()
        {
            Log.Logger.DebugFormat("Executing ==> public static IEnumerable<LuceneIndexItem> GetAllIndexedRecords()");
            ValidateIndex();
            var results = new SearchResults(Store.GetAllIndexedRecords()); 
            Log.Logger.DebugFormat("     Exit ==> public static IEnumerable<LuceneIndexItem> GetAllIndexedRecords().  Returning {0} records", results.TotalResults);
            return results;
        }

        #endregion

        #region Index

        /// <summary>
        /// Reindex all portal files.
        /// </summary>
        internal void IndexAll()
        {
            IndexFiles(null);
            Log.Logger.DebugFormat("Exiting ReIndexContent");
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Indexes content of all portals within the given time frame
        /// </summary>
        /// <history>
        ///     [vnguyen]   04/17/2013  created
        /// </history>
        /// -----------------------------------------------------------------------------
        internal void IndexContent(DateTime startDate)
        {
            ValidateIndex();
            IndexFiles(startDate);
        }

        private void IndexFiles(DateTime? startDate)
        {
            var fileIndexer = new FileIndexer();

            if (!startDate.HasValue)
                if (!Store.DeleteAll())
                    return;

            var portals = PortalController.Instance.GetPortals();
            foreach (var portal in portals.Cast<PortalInfo>())
            {
                if (!startDate.HasValue)
                {
                    Log.Logger.InfoFormat("Reindexing all documents from Portal {0}", portal.PortalID);
                }
                var indexSince = FixedIndexingStartDate(portal.PortalID, startDate ?? DateTime.MinValue);
                List<LuceneIndexItem> searchDocs = fileIndexer.GetPortalSearchDocuments(portal.PortalID, indexSince).ToList();
                Log.Logger.DebugFormat("Found {1} documents from Portal {0} to index", portal.PortalID, searchDocs.Count());
                Store.AddOld(searchDocs);
                Log.Logger.DebugFormat("Indexed {1} documents from Portal {0}", portal.PortalID, searchDocs.Count());
            }
            Store.OptimizeOld();


            //using (LuceneController lc = LuceneController.Instance)
            //{
            //    var portals = PortalController.Instance.GetPortals();
            //    foreach (var portal in portals.Cast<PortalInfo>())
            //    {
            //        if (!startDate.HasValue)
            //        {
            //            Log.Logger.InfoFormat("Reindexing all documents from Portal {0}", portal.PortalID);
            //        }
            //        lc.Store.Delete(new TermQuery(new Term("$type", portal.PortalID.ToString())));

            //        var indexSince = FixedIndexingStartDate(portal.PortalID, startDate ?? DateTime.MinValue);
            //        List<LuceneIndexItem> searchDocs = fileIndexer.GetPortalSearchDocuments(portal.PortalID, indexSince).ToList();
            //        Log.Logger.DebugFormat("Found {1} documents from Portal {0} to index", portal.PortalID, searchDocs.Count());
            //        foreach (var indexItem in searchDocs)
            //        {
            //            lc.Add(indexItem, indexConfig);
            //        }
            //        Store.AddOld(searchDocs);
            //        Log.Logger.DebugFormat("Indexed {1} documents from Portal {0}", portal.PortalID, searchDocs.Count());
            //    }
            //    lc.Store.Commit();
            //    lc.Store.OptimizeSearchIndex(true);
            //    LuceneController.ClearInstance();
            //}

        }

        #endregion

        #region Operations

        public void Add(LuceneIndexItem data, FieldConfig config)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }

            _serviceInstance.Add(DnnFilesMappingUtils.DataItemToLuceneDocument(data.PortalId.ToString(),data.FileId.ToString(), data, config));
        }

        public void Update(LuceneIndexItem data, FieldConfig config)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }
            Delete(data);
            Add(data, config);
        }

        /// <summary>
        /// Deletes the matching objects in the IndexWriter.
        /// </summary>
        /// <param name="data"></param>
        public void Delete(LuceneIndexItem data)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }

            //var selection = new TermQuery(new Term(JsonMappingUtils.FieldId, data.ContentId.ToString()));
            var selection = new TermQuery(new Term(DnnFilesMappingUtils.GetIndexFieldName(), DnnFilesMappingUtils.GetIndexFieldValue(data)));

            Query deleteQuery = new FilteredQuery(selection, DnnFilesMappingUtils.GetTypeFilter(data.PortalId.ToString()));
            _serviceInstance.Delete(deleteQuery);
        }

        public void DeleteOld(int fileId)
        {
            Log.Logger.DebugFormat("Executing ==> public static void RemoveDocument(int [{0}])", fileId);
            Store.DeleteOld(fileId);
            Log.Logger.DebugFormat("     Exit ==> public static void RemoveDocument(int [{0}])", fileId);
        }

        private void ValidateIndex()
        {
            var reindexer = new Action(delegate()
            {
                var indexer = new LuceneController();
                indexer.IndexAll();
            });

            _serviceInstance.Initialise(reindexer);
            Log.Logger.DebugFormat("Exiting ValidateIndex");
        }

        #endregion

        #region Private

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Adjusts the re-index date/time to account for the portal reindex value
        /// </summary>
        /// -----------------------------------------------------------------------------
        private static DateTime FixedIndexingStartDate(int portalId, DateTime startDate)
        {
            if (startDate < SqlDateTime.MinValue.Value || SearchHelper.Instance.IsReindexRequested(portalId, startDate))
            {
                return SqlDateTime.MinValue.Value.AddDays(1);
            }
            return startDate;
        }

        #endregion

        public void Dispose()
        {
            _serviceInstance.Dispose();
            _serviceInstance = null;
        }

    }
}