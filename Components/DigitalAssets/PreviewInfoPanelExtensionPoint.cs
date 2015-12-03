using DotNetNuke.ExtensionPoints;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web;

namespace Satrabel.OpenFiles.Components.DigitalAssets
{
    [Export(typeof(IUserControlExtensionPoint))]
    [ExportMetadata("Module", "DigitalAssets")]
    [ExportMetadata("Name", "PreviewInfoPanelExtensionPoint")]
    [ExportMetadata("Group", "ViewProperties")]
    [ExportMetadata("Priority", 1)]
    public class PreviewInfoPanelExtensionPoint : IUserControlExtensionPoint
    {
        public string UserControlSrc
        {
            get { return "~/DesktopModules/OpenFiles/DigitalAssets/PreviewPanelControl.ascx"; }
        }

        public string Text
        {
            get { return ""; }
        }

        public string Icon
        {
            get { return ""; }
        }

        public int Order
        {
            get { return 1; }
        }

        public bool Visible
        {
            get { return true; }
        }
    }
}