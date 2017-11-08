using System;
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

        public FolderUri SchemaFolder => new FolderUri("~/DesktopModules/OpenFiles/Templates/Schema/");

        public FolderUri PortalFolder(int portalId, string portalHomeDirectory)
        {
            if (portalId > -1)
                return new PortalFolderUri(portalId, portalHomeDirectory + "/OpenFiles/");
            Log.Logger.WarnFormat("Cannot determine PortalFolder as Portalsettings is NULL");
            return null;
        }

        public bool CaseSensitiveFieldNames => false;
        public string LuceneIndexFolder => @"App_Data\OpenFiles\LuceneIndex";


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