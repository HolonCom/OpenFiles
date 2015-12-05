using System;
using System.ComponentModel.Composition;

using DotNetNuke.ExtensionPoints;

namespace Satrabel.Modules.DigitalAssets.Components.ExtensionPoint.UserControls
{
    [Export(typeof(IUserControlExtensionPoint))]
    [ExportMetadata("Module", "DigitalAssets")]
    [ExportMetadata("Name", "FileFieldsControlExtensionPoint")]
    [ExportMetadata("Group", "ViewProperties")]
    [ExportMetadata("Priority", 1)]
    public class FileFieldsControlExtensionPoint : IUserControlExtensionPoint
    {
        public string UserControlSrc
        {
            get { return "~/DesktopModules/OpenFiles/DigitalAssets/FileFieldsControl.ascx"; }
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