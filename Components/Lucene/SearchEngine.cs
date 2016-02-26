#region Usings

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Search.Entities;
using DotNetNuke.Services.Search.Internals;
using DotNetNuke.Services.Scheduling;
using Newtonsoft.Json;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Satrabel.OpenContent.Components;

#endregion

namespace Satrabel.OpenFiles.Components.Lucene
{
    internal class SearchEngine
    {

        public int IndexedSearchDocumentCount { get; private set; }

        public Dictionary<string, int> Results { get; private set; }

        public static List<LuceneIndexItem> GetAllIndexedRecords()
        {
            Log.Logger.DebugFormat("Executing ==> public static IEnumerable<LuceneIndexItem> GetAllIndexedRecords()");
            ValidateIndex();
            List<LuceneIndexItem> results = LuceneService.GetAllIndexedRecords();
            Log.Logger.DebugFormat("     Exit ==> public static IEnumerable<LuceneIndexItem> GetAllIndexedRecords().  Returning {0} records", results.Count);
            return results;
        }

        public static List<LuceneIndexItem> Search(string input, string fieldName = "")
        {
            Log.Logger.DebugFormat("Executing ==> public static IEnumerable<LuceneIndexItem> Search(string [{0}], string [{1}])", input, fieldName);
            List<LuceneIndexItem> retval;
            ValidateIndex();
            if (string.IsNullOrEmpty(input))
                retval = GetAllIndexedRecords();
            else
                retval = LuceneService.DoSearch(input, fieldName);
            Log.Logger.DebugFormat("     Exit ==> public static IEnumerable<LuceneIndexItem> Search() with {0} items found", retval.Count);
            return retval;
        }

        public static void RemoveDocument(int fileId)
        {
            Log.Logger.DebugFormat("Executing ==> public static void RemoveDocument(int [{0}])", fileId);
            LuceneService.RemoveLuceneIndexRecord(fileId);
            Log.Logger.DebugFormat("     Exit ==> public static void RemoveDocument(int [{0}])", fileId);
        }

        #region Private


        private static void ValidateIndex()
        {
            if (LuceneService.IndexExists()) return;

            var indexer = new SearchEngine();
            indexer.ReIndexContent();
        }

        /// <summary>
        /// Reindex all portal files.
        /// </summary>
        internal void ReIndexContent()
        {
            IndexFiles(null);
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
            IndexFiles(startDate);
        }

        private void IndexFiles(DateTime? startDate)
        {
            var fileIndexer = new FileIndexer();
            IndexedSearchDocumentCount = 0;
            Results = new Dictionary<string, int>();

            if (!startDate.HasValue)
                if (!LuceneService.ClearLuceneIndex())
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
                LuceneService.IndexItem(searchDocs);
                IndexedSearchDocumentCount += searchDocs.Count();
                Log.Logger.DebugFormat("Indexed {1} documents from Portal {0}", portal.PortalID, searchDocs.Count());
            }
            if (IndexedSearchDocumentCount > 10)
                LuceneService.Optimize();
            Results.Add("Files", IndexedSearchDocumentCount);
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
    }
}