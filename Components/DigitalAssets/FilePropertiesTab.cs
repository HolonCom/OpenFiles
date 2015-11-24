using System.ComponentModel.Composition;

using DotNetNuke.Common.Utilities;
using DotNetNuke.ExtensionPoints;
using DotNetNuke.Services.Localization;

namespace Satrabel.Modules.DigitalAssets.Components.ExtensionPoint.UserControls
{
    
    [Export(typeof(IEditPageTabExtensionPoint))]
    [ExportMetadata("Module", "DigitalAssets")]
    [ExportMetadata("Name", "FilePropertiesTab1")]
    [ExportMetadata("Group", "FilePropertiesTab")]
    [ExportMetadata("Priority", 1)]
    
    public class FilePropertiesTab : IEditPageTabExtensionPoint
    {
        private const string _localResourceFile = "DesktopModules/OpenDocument/DigitalAssets/App_LocalResources/FilePropertiesTabControl.ascx.resx";

        public string UserControlSrc
        {
            get { return "~/DesktopModules/OpenDocument/DigitalAssets/FilePropertiesTabControl.ascx"; }
        }

        public string Text
        {
            get { return Localization.GetString("TabText", _localResourceFile); }
        }

        public string Icon
        {
            get { return ""; }
        }

        public int Order
        {
            get { return 1; }
        }

        public string EditPageTabId
        {
            get { return "ssAdvancedUrlSettings"; }
        }

        public string CssClass
        {
            get { return ""; }
        }

        public string Permission
        {
            get { return ""; }
        }

        public bool Visible
        {
            get { return (Config.GetFriendlyUrlProvider() == "advanced"); }
        }
    }
}