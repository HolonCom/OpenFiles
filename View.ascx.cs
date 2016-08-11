#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Common.Utilities;
using Satrabel.OpenFiles.Components.Lucene;
using DotNetNuke.Services.Scheduling;
using DotNetNuke.Services.FileSystem;
using System.Web.UI.WebControls;
using System.Collections.Generic;

#endregion

namespace Satrabel.OpenFiles
{
    public partial class View : PortalModuleBase
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Page.IsPostBack)
            {
                bIndex.Visible = ModuleContext.PortalSettings.UserInfo.IsSuperUser;

                var fm = FolderManager.Instance;
                var folders = fm.GetFolders(PortalId);
                foreach (var folder in folders)
                {
                    ddlFolders.Items.Add(new ListItem(folder.FolderPath.Trim('/'), folder.FolderPath));
                }
            }
        }

        protected void bIndex_Click(object sender, EventArgs e)
        {
            var searchEngine = LuceneController.Instance;
            searchEngine.IndexAll();
        }
        protected void bScheduleTask_Click(object sender, EventArgs e)
        {
            var sc = SchedulingProvider.Instance();
            var schedule = sc.GetSchedule("Satrabel.OpenFiles.Components.Lucene.SearchEngineScheduler, OpenFiles", "");
            if (schedule == null)
            {
                schedule = CreateScheduleItem();
                SchedulingProvider.Instance().AddSchedule(schedule);
            }
        }

        private ScheduleItem CreateScheduleItem()
        {
            var scheduleItem = new ScheduleItem();
            scheduleItem.TypeFullName = "Satrabel.OpenFiles.Components.Lucene.SearchEngineScheduler, OpenFiles";
            scheduleItem.FriendlyName = "OpenFiles.Search";
            //DNN-4964 - values for time lapse and retry frequency can't be set to 0, -1 or left empty (client side validation has been added)
            scheduleItem.TimeLapse = 30;
            scheduleItem.TimeLapseMeasurement = "m";
            scheduleItem.RetryTimeLapse = 30;
            scheduleItem.RetryTimeLapseMeasurement = "m";
            scheduleItem.RetainHistoryNum = 60;
            scheduleItem.AttachToEvent = "";
            scheduleItem.CatchUpEnabled = false;
            scheduleItem.Enabled = true;
            scheduleItem.ObjectDependencies = "SearchEngine";
            scheduleItem.ScheduleStartDate = Null.NullDate;
            scheduleItem.Servers = "";
            return scheduleItem;
        }

        protected void bIndexFolder_Click(object sender, EventArgs e)
        {
            var searchEngine = LuceneController.Instance;
            searchEngine.IndexFolder(PortalId, ddlFolders.SelectedValue);
        }
    }
}