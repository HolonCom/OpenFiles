#region Usings

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Search.Internals;
using Lucene.Net.Documents;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Lucene.Mapping;

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
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
            }
            _instance = new LuceneController();
        }
        #endregion

        #region Search

        internal SearchResults Search(SelectQueryDefinition def)
        {
            if (!Store.ValidateIndexFolder())
            {
                IndexAll();
                return new SearchResults();
            }
            Func<Document, LuceneIndexItem> resultMapper = DnnFilesMappingUtils.MapLuceneDocumentToData;
            var luceneResults = Store.Search(def.Filter, def.Query, def.Sort, def.PageSize, def.PageIndex, resultMapper);
            return luceneResults;
        }

        #endregion

        #region Index

        /// <summary>
        /// Reindex all portal files.
        /// </summary>
        private void IndexAll()
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
            if (!Store.ValidateIndexFolder())
                IndexAll();
            else
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

                    var fileIndexer = new DnnFilesRepository();
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
                            lc.Store.Delete(new TermQuery(new Term(DnnFilesMappingUtils.GetIndexFieldName(), DnnFilesMappingUtils.GetIndexFieldValue(indexItem))));
                            lc.Store.Add(DnnFilesMappingUtils.DataItemToLuceneDocument(indexItem));
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

            Store.Add(DnnFilesMappingUtils.DataItemToLuceneDocument(data));
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
            if (_serviceInstance != null)
            {
                _serviceInstance.Dispose();
                _serviceInstance = null;
            }
        }
    }
}