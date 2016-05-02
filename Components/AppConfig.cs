using System;
using DotNetNuke.Entities.Portals;
using Satrabel.OpenContent.Components;

namespace Satrabel.OpenFiles.Components
{

    public class AppConfig
    {
        private static readonly Lazy<AppConfig> lazy = new Lazy<AppConfig>(() => new AppConfig());

        public static AppConfig Instance { get { return lazy.Value; } }

        private AppConfig()
        {
            var ps = PortalSettings.Current;
            PortalFolder = new PortalFolderUri(ps.PortalId, ps.HomeDirectory + "/OpenFiles/");
        }

        public FolderUri SchemaFolder
        {
            get { return new FolderUri("~/DesktopModules/OpenFiles/Templates/Schema/"); }
        }
        public FolderUri PortalFolder { get; private set; }

        public bool CaseSensitiveFieldNames { get { return false; } }
        public string LuceneIndexFolder { get { return @"App_Data\OpenFiles\LuceneIndex"; } }
    }
}