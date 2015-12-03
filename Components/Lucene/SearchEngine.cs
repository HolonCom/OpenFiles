#region Usings

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
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

#endregion

namespace Satrabel.OpenFiles.Components.Lucene
{
    internal class SearchEngine
    {
        public int IndexedSearchDocumentCount { get; private set; }

        public Dictionary<string, int> Results { get; private set; }

        public int DeletedCount { get; private set; }

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

        /// <summary>
        /// Reindex all portal files.
        /// </summary>
        internal void ReIndexContent()
        {
            IndexFiles(null);
        }

        public static IEnumerable<LuceneIndexItem> GetAllIndexedRecords()
        {
            return LuceneService.GetAllIndexedRecords();
        }

        public static IEnumerable<LuceneIndexItem> Search(string input, string fieldName = "")
        {
            if (String.IsNullOrEmpty(input)) return new List<LuceneIndexItem>();

            if(LuceneService.IndexNeedInitialization())
            {
                var indexer = new SearchEngine();
                indexer.ReIndexContent();
            }

            return LuceneService.DoSearch(input, fieldName);
        }

        public static void RemoveDocument(int fileId)
        {
            LuceneService.RemoveLuceneIndexRecord(fileId);
        }

        #region Private

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
                var indexSince = FixedIndexingStartDate(portal.PortalID, startDate ?? DateTime.MinValue);
                var searchDocs = fileIndexer.GetPortalSearchDocuments(portal.PortalID, indexSince);
                LuceneService.IndexItem(searchDocs);
                IndexedSearchDocumentCount += searchDocs.Count();
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