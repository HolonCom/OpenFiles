#region Usings

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Lucene.Net.Search;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Search.Internals;
using Lucene.Net.Documents;
using Satrabel.OpenContent.Components.Lucene;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Utils;

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
            _serviceInstance = new LuceneService(AppConfig.Instance.LuceneIndexFolder, LuceneMappingUtils.GetAnalyser());
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
            Func<Document, LuceneIndexItem> resultMapper = LuceneMappingUtils.CreateLuceneItem;
            var luceneResults = Store.Search(def.Filter, def.Query, def.Sort, def.PageSize, def.PageIndex, resultMapper);
            return luceneResults;
        }

        #endregion

        #region Index

        /// <summary>
        /// Reindex files of a Folder.
        /// </summary>
        public void ReIndexFolderFiles(string folder)
        {

        }

        /// <summary>
        /// Reindex all portal files.
        /// </summary>
        internal void IndexAll()
        {
            Log.Logger.InfoFormat("Lucene index all [{0}] being initialized.", "OpenFiles");
            IndexClear();
            IndexFiles(null);
            Log.Logger.InfoFormat("Exiting ReIndex All");
        }

        /// <summary>
        /// Reindex all files of folder.
        /// </summary>
        internal void IndexFolder(int portalId, string folderPath)
        {
            Log.Logger.InfoFormat("Lucene index folder [{0}] being initialized.", "OpenFiles");
            DeleteFolder(portalId, folderPath, null);
            IndexFolder(null, portalId, folderPath);
            Log.Logger.InfoFormat("Exiting ReIndex Folder");
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

        private void IndexFiles(DateTime? startDate, string folderPath = "")
        {
            LuceneController.ClearInstance();
            try
            {
                using (var lc = LuceneController.Instance)
                {
                    var fileIndexer = new DnnFilesRepository();
                    var portals = PortalController.Instance.GetPortals();
                    foreach (var portal in portals.Cast<PortalInfo>())
                    {
                        if (!startDate.HasValue)
                        {
                            Log.Logger.InfoFormat("Reindexing all documents from Portal {0}", portal.PortalID);
                        }
                        var indexSince = FixedIndexingStartDate(portal.PortalID, startDate ?? DateTime.MinValue);
                        List<LuceneIndexItem> searchDocs = fileIndexer.GetPortalSearchDocuments(portal.PortalID, "", true, indexSince).ToList();
                        Log.Logger.DebugFormat("Found {1} documents from Portal {0} to index", portal.PortalID, searchDocs.Count());

                        foreach (LuceneIndexItem indexItem in searchDocs)
                        {
                            Delete(indexItem, lc);
                            FieldConfig indexJson = FilesRepository.GetIndexConfig(portal);
                            lc.Store.Add(LuceneMappingUtils.CreateLuceneDocument(indexItem, indexJson));
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

        private void IndexFolder(DateTime? startDate, int portalId, string folderPath)
        {
            LuceneController.ClearInstance();
            try
            {
                using (var lc = LuceneController.Instance)
                {
                    var fileIndexer = new DnnFilesRepository();
                    if (!startDate.HasValue)
                    {
                        Log.Logger.InfoFormat("Reindexing documents from Portal {0} folder {1}", portalId, folderPath);
                    }
                    var indexSince = FixedIndexingStartDate(portalId, startDate ?? DateTime.MinValue);
                    List<LuceneIndexItem> searchDocs = fileIndexer.GetPortalSearchDocuments(portalId, folderPath, true, indexSince).ToList();
                    Log.Logger.DebugFormat("Found {2} documents from Portal {0} folder {1} to index", portalId, folderPath, searchDocs.Count());

                    FieldConfig indexJson = FilesRepository.GetIndexConfig(portalId);
                    foreach (LuceneIndexItem indexItem in searchDocs)
                    {
                        Delete(indexItem, lc);
                        lc.Store.Add(LuceneMappingUtils.CreateLuceneDocument(indexItem, indexJson));
                    }
                    Log.Logger.DebugFormat("Indexed {2} documents from Portal {0} folder {1}", portalId, folderPath, searchDocs.Count());
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

        public void Update(LuceneIndexItem data)
        {
            this.Delete(data, null);
            this.Add(data, null);
        }

        //private void Add(LuceneIndexItem data)
        //{
        //    this.Add(data, null);
        //}

        /// <summary>
        /// Deletes the matching objects in the IndexWriter.
        /// </summary>
        /// <param name="data"></param>
        public void Delete(LuceneIndexItem data)
        {
            Delete(data, null);
        }

        #endregion

        #region Private

        private void Add(LuceneIndexItem data, LuceneController storeInstance)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }

            FieldConfig indexJson = FilesRepository.GetIndexConfig(data.PortalId);
            Store.Add(LuceneMappingUtils.CreateLuceneDocument(data, indexJson));
        }

        private void Update(LuceneIndexItem data, LuceneController storeInstance)
        {
            if (null == data)
            {
                throw new ArgumentNullException("data");
            }
            Delete(data, storeInstance);
            Add(data, storeInstance);
        }

        private void Delete(LuceneIndexItem data, LuceneController storeInstance)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            Query deleteQuery = LuceneMappingUtils.GetDeleteQuery(data);
            if (storeInstance == null)
                Store.Delete(deleteQuery);
            else
                storeInstance.Store.Delete(deleteQuery);
        }

        private void DeleteFolder(int portalId, string folderPath, LuceneController storeInstance)
        {
            Query deleteQuery = LuceneMappingUtils.GetDeleteFolderQuery(portalId, folderPath);
            if (storeInstance == null)
                Store.Delete(deleteQuery);
            else
                storeInstance.Store.Delete(deleteQuery);
        }

        private void IndexClear()
        {
            LuceneController.ClearInstance();
            try
            {
                using (var lc = LuceneController.Instance)
                {
                    lc.Store.DeleteAll();
                }
            }
            finally
            {
                LuceneController.ClearInstance();
            }
        }

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