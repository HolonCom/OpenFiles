using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Modules.DigitalAssets.Components.Controllers;
using DotNetNuke.Modules.DigitalAssets.Components.Controllers.Models;
using DotNetNuke.Modules.DigitalAssets.Components.ExtensionPoint;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.Client.ClientResourceManagement;
using DotNetNuke.Web.Client;
using System.Web.Hosting;
using Satrabel.OpenContent.Components.Alpaca;
using Satrabel.OpenFiles.Components.DigitalAssets;

namespace Satrabel.Modules.DigitalAssets
{
    public partial class FileFieldsControl : DotNetNuke.Modules.DigitalAssets.FileFieldsControl, IFieldsControl
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            string virtualFolder = "DesktopModules/OpenFiles/";
            
            string portalFolder = HostingEnvironment.MapPath(PortalSettings.HomeDirectory + "/OpenFiles/");
            if (!Directory.Exists(portalFolder))
            {
                Directory.CreateDirectory(portalFolder);
            }
            
            string schemaFilename = portalFolder + "schema.json";
            if (System.IO.File.Exists(schemaFilename))
            {
                virtualFolder = PortalSettings.HomeDirectory + "OpenFiles/";
            }
            AlpacaEngine alpaca = new AlpacaEngine(Page, ModuleContext, virtualFolder, "");
            alpaca.RegisterAll();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Page.IsPostBack)
            {
            }
        }
        public override void PrepareProperties()
        {
            base.PrepareProperties();
            //TitleInput.Text = File.Title;
        }
        public override object SaveProperties()
        {
            var file = base.SaveProperties();
            //File.Title = TitleInput.Text;
            ContentItemUtils.Save(File, "meta", hfAlpacaData.Value);
            return file;
        }
        public int ContentItemId
        {
            get
            {
                return File.ContentItemID;
            }
        }
        public string CurrentCulture
        {
            get
            {
                return LocaleController.Instance.GetCurrentLocale(PortalId).Code;
            }
        }
        public string NumberDecimalSeparator
        {
            get
            {
                return LocaleController.Instance.GetCurrentLocale(PortalId).Culture.NumberFormat.NumberDecimalSeparator;
            }
        }
    }
}