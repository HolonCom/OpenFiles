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
            PortalFolder = new FolderUri(DotNetNuke.Common.Globals.GetPortalSettings().HomeDirectory + "/OpenFiles/");
        }

        public FolderUri DesktopModulesFolder
        {
            get { return new FolderUri("~/DesktopModules/OpenFiles/Templates/Schema/"); }
        }
        public FolderUri PortalFolder { get; private set; }
    }
}