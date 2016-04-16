using System.Web.Hosting;
using DotNetNuke.Entities.Portals;

namespace Satrabel.OpenFiles.Components
{
    public static class Config
    {
        public static string JsonSchemaFolder
        {
            get
            {
                return HostingEnvironment.MapPath("~/DesktopModules/OpenFiles/Templates/Schema/");
            }
        }

        public static string PortalFolder(PortalSettings portalSettings)
        {
            {
                return HostingEnvironment.MapPath(portalSettings.HomeDirectory + "/OpenFiles/");
            }
        }
    }
}