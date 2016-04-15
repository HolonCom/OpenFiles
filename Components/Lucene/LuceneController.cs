﻿#region Usings

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
                {
                    throw new Exception("LuceneController not initialized properly");
                }
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
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
            }
            _instance = new LuceneController();
        }
        #endregion

        #region Search

        internal SearchResults Search(Query filter, Query query, Sort sort, int pageSize, int pageIndex)
        {
            ValidateIndex();
            var searcher = Store.GetSearcher();
            TopDocs topDocs;
            if (filter == null)
                topDocs = searcher.Search(query, null, (pageIndex + 1) * pageSize, sort);
            else
                topDocs = searcher.Search(query, new QueryWrapperFilter(filter), (pageIndex + 1) * pageSize, sort);
            var results = MapLuceneToDataList(topDocs.ScoreDocs.Skip(pageIndex * pageSize), searcher);
            var luceneResults = new SearchResults(results, topDocs.TotalHits);
            return luceneResults;
        }

        private static List<LuceneIndexItem> MapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            // v 2.9.4: use 'hit.doc'
            // v 3.0.3: use 'hit.Doc'
            return hits.Select(hit => DnnFilesMappingUtils.MapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList(); //todo param
        }
        #endregion

        #region Index

        /// <summary>
        /// Reindex all portal files.
        /// </summary>
        internal void IndexAll()
        {
            Log.Logger.DebugFormat("Lucene index directory [{0}] being initialized.", "OpenFiles");
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
            LuceneController.ClearInstance();
            try
            {
                using (var lc = LuceneController.Instance)
                {
                    if (!startDate.HasValue)
                        lc.Store.DeleteAll();

                    var fileIndexer = new FileIndexer();
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

                        foreach (var indexItem in searchDocs)
                        {
                            lc.Store.Add(DnnFilesMappingUtils.DataItemToLuceneDocument(indexItem.PortalId.ToString(), indexItem.FileId.ToString(), indexItem));
                        }
                        Log.Logger.DebugFormat("Indexed {1} documents from Portal {0}", portal.PortalID, searchDocs.Count());
                    }
                    lc.Store.Commit();
                    lc.Store.OptimizeSearchIndex(true);
                }
            }
            finally
            {
                LuceneController.ClearInstance();
            }
        }

        #endregion

        #region Operations

        public void Add(LuceneIndexItem data)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }

            Store.Add(DnnFilesMappingUtils.DataItemToLuceneDocument(data.PortalId.ToString(), data.FileId.ToString(), data));
        }

        public void Update(LuceneIndexItem data)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }
            Delete(data);
            Add(data);
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
            Delete(int.Parse(DnnFilesMappingUtils.GetIndexFieldValue(data)), data.PortalId);
        }

        public void Delete(int fileId, int portalId)
        {
            var selection = new TermQuery(new Term(DnnFilesMappingUtils.GetIndexFieldName(), fileId.ToString()));
            Query deleteQuery = new FilteredQuery(selection, DnnFilesMappingUtils.GetTypeFilter(portalId.ToString()));
            Store.Delete(deleteQuery);
        }

        private void ValidateIndex()
        {
            if (Store.ValidateIndexFolder())
                return;
            Instance.IndexAll();
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