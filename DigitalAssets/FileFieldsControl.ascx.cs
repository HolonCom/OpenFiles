using System;
using System.IO;
using System.Web.Hosting;
using DotNetNuke.Modules.DigitalAssets.Components.ExtensionPoint;
using DotNetNuke.Services.Localization;
using Satrabel.OpenContent.Components.Alpaca;
using Satrabel.OpenFiles.Components;

namespace Satrabel.OpenFiles.DigitalAssets
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