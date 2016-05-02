using System;
using System.Web.Hosting;
using DotNetNuke.Entities.Portals;
using Satrabel.OpenContent.Components;

namespace Satrabel.OpenFiles.Components
{

    public class Config
    {
        private static readonly Lazy<Config> lazy = new Lazy<Config>(() => new Config());

        public static Config Instance { get { return lazy.Value; } }

        private Config()
        {
            var ps = PortalSettings.Current;
            PortalFolder = new PortalFolderUri(ps.PortalId, ps.HomeDirectory + "/OpenFiles/");
        }

        public FolderUri SchemaFolder
        {
            get { return new FolderUri("~/DesktopModules/OpenFiles/Templates/Schema/"); }
        }
        public FolderUri PortalFolder { get; private set; }
    }
}