using System;
using DotNetNuke.Entities.Portals;
using Satrabel.OpenContent.Components;

namespace Satrabel.OpenFiles.Components
{

    public class AppConfig
    {
        private static readonly Lazy<AppConfig> lazy = new Lazy<AppConfig>(() => new AppConfig());
        public static AppConfig Instance => lazy.Value;

        private AppConfig()
        {
        }

        public FolderUri SchemaFolder
        {
            get { return new FolderUri("~/DesktopModules/OpenFiles/Templates/Schema/"); }
        }
        public FolderUri PortalFolder(PortalSettings ps)
        {
            //var ps = PortalSettings.Current;
            if (ps != null)
                return new PortalFolderUri(ps.PortalId, ps.HomeDirectory + "/OpenFiles/");
            Log.Logger.WarnFormat("Cannot determine PortalFolder as Portalsettings is NULL");
            return null;
        }

        public FolderUri PortalFolder(PortalInfo ps)
        {
            if (ps != null)
                return new PortalFolderUri(ps.PortalID, ps.HomeDirectory + "/OpenFiles/");
            Log.Logger.WarnFormat("Cannot determine PortalFolder as PortalInfo is NULL");
            return null;
        }

        public bool CaseSensitiveFieldNames { get { return false; } }
        public string LuceneIndexFolder { get { return @"App_Data\OpenFiles\LuceneIndex"; } }


        #region Constants

        internal static string FieldNamePublishStartDate
        {
            get
            {
                const string CONSTANT = "publishstartdate";
                return CONSTANT;
            }
        }

        internal static string FieldNamePublishEndDate
        {
            get
            {
                const string CONSTANT = "publishenddate";
                return CONSTANT;
            }
        }

        internal static string FieldNamePublishStatus
        {
            get
            {
                const string CONSTANT = "publishstatus";
                return CONSTANT;
            }
        }

        #endregion
    }
}