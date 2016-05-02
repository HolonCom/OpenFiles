using System;
using System.Web.UI.WebControls;
using DotNetNuke.ExtensionPoints;
using DotNetNuke.Modules.DigitalAssets.Components.ExtensionPoint;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Localization;
using Satrabel.OpenContent.Components.Alpaca;
using Satrabel.OpenFiles.Components;

namespace Satrabel.OpenFiles.DigitalAssets
{
    public partial class FilePropertiesTabControl : PropertiesTabContentControl, IEditPageTabControlActions
    {
        protected IFileInfo File { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            AlpacaEngine alpaca = new AlpacaEngine(Page, ModuleContext, Config.Instance.SchemaFolder.FolderPath, "images");
            alpaca.RegisterAll();

            int fileid = int.Parse(Page.Request.QueryString["fileId"]);
            var fm = DotNetNuke.Services.FileSystem.FileManager.Instance;
            File = fm.GetFile(fileid);
            ScopeWrapper.Visible = fm.IsImageFile(File);
            lblNoImage.Visible = !ScopeWrapper.Visible;
        }

        public void BindAction(int portalId, int tabId, int moduleId)
        {

        }

        public void CancelAction(int portalId, int tabId, int moduleId)
        {

        }

        public void SaveAction(int portalId, int tabId, int moduleId)
        {

        }

        public override void DataBindItem()
        {

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
        public string ImageUrl
        {
            get
            {
                var fm = DotNetNuke.Services.FileSystem.FileManager.Instance;
                return fm.GetUrl(File);
            }
        }

        protected void validation_ServerValidate(object source, ServerValidateEventArgs args)
        {
            ContentItemUtils.Save(File, "crop", hfAlpacaImagesData.Value);
        }
    }
}