using System;
using System.IO;
using System.Text;
using DotNetNuke.Modules.DigitalAssets.Components.ExtensionPoint;
using DotNetNuke.Services.Localization;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Alpaca;
using Satrabel.OpenFiles.Components;
using Satrabel.OpenFiles.Components.Lucene;
using Satrabel.OpenFiles.Components.Utils;
using AppConfig = Satrabel.OpenFiles.Components.AppConfig;

namespace Satrabel.OpenFiles.DigitalAssets
{
    public partial class FileFieldsControl : DotNetNuke.Modules.DigitalAssets.FileFieldsControl, IFieldsControl
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            var virtualFolderOfSchemaFiles = AppConfig.Instance.SchemaFolder;

            var portalFolder = AppConfig.Instance.PortalFolder(PortalSettings);
            if (!portalFolder.FolderExists)
            {
                Directory.CreateDirectory(portalFolder.PhysicalFullDirectory);
            }

            var schemaFile = new FileUri(portalFolder, "schema.json");
            if (schemaFile.FileExists)
            {
                virtualFolderOfSchemaFiles = AppConfig.Instance.PortalFolder(PortalSettings);
            }

            AlpacaEngine alpaca = new AlpacaEngine(Page, ModuleContext, virtualFolderOfSchemaFiles.FolderPath, "");
            alpaca.RegisterAll(false, false);
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
        }

        public override object SaveProperties()
        {
            var file = base.SaveProperties();
            OpenFilesUtils.Save(File, LuceneMappingUtils.MetaField, hfAlpacaData.Value);
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