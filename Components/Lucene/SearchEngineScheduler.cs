#region Usings

using System;
using DotNetNuke.Common;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Scheduling;
using DotNetNuke.Services.Search.Internals;
using DotNetNuke.Services.Exceptions;

#endregion

namespace Satrabel.OpenDocument.Components.Lucene
{
  
    public class SearchEngineScheduler : SchedulerClient
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(Globals));

        public SearchEngineScheduler(ScheduleHistoryItem objScheduleHistoryItem)
        {
            ScheduleHistoryItem = objScheduleHistoryItem;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// DoWork runs the scheduled item
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///		[cnurse]	11/15/2004	documented
        ///     [vqnguyen]  05/28/2013  updated
        /// </history>
        /// -----------------------------------------------------------------------------
        public override void DoWork()
        {
            try
            {
                var lastSuccessFulDateTime = SearchHelper.Instance.GetLastSuccessfulIndexingDateTime(ScheduleHistoryItem.ScheduleID);
                Logger.Trace("Search: File Crawler - Starting. Content change start time " + lastSuccessFulDateTime.ToString("g"));
                ScheduleHistoryItem.AddLogNote(string.Format("Starting. Content change start time <b>{0:g}</b>", lastSuccessFulDateTime));

                var searchEngine = new SearchEngine();
                try
                {
                    searchEngine.IndexContent(lastSuccessFulDateTime);
                    foreach (var result in searchEngine.Results)
                    {
                        ScheduleHistoryItem.AddLogNote(string.Format("<br/>&nbsp;&nbsp;{0} Indexed: {1}", result.Key, result.Value));
                    }
                                        
                }
                finally
                {
                    //searchEngine.Commit();
                }

                ScheduleHistoryItem.Succeeded = true;
                ScheduleHistoryItem.AddLogNote("<br/><b>Indexing Successful</b>");
                SearchHelper.Instance.SetLastSuccessfulIndexingDateTime(ScheduleHistoryItem.ScheduleID, ScheduleHistoryItem.StartDate);

                Logger.Trace("Search: File Crawler - Indexing Successful");
            }
            catch (Exception ex)
            {
                ScheduleHistoryItem.Succeeded = false;
                ScheduleHistoryItem.AddLogNote("<br/>EXCEPTION: " + ex.Message);
                Errored(ref ex);
                if (ScheduleHistoryItem.ScheduleSource != ScheduleSource.STARTED_FROM_BEGIN_REQUEST)
                {
                    Exceptions.LogException(ex);
                }
            }
        }
    }
}