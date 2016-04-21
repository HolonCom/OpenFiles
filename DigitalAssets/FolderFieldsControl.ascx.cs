using System;
using System.Linq;
using System.Web.Hosting;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Modules.DigitalAssets.Components.Controllers;
using DotNetNuke.Modules.DigitalAssets.Components.Controllers.Models;
using DotNetNuke.Modules.DigitalAssets.Components.ExtensionPoint;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.Client;
using DotNetNuke.Web.Client.ClientResourceManagement;

namespace Satrabel.OpenFiles.DigitalAssets
{
    public partial class FolderFieldsControl : PortalModuleBase /*DotNetNuke.Modules.DigitalAssets.FileFieldsControl*/ , IFieldsControl
    {
        protected IFolderInfo Folder { get; private set; }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ServicesFramework.Instance.RequestAjaxScriptSupport();
            ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
            DotNetNuke.UI.Utilities.ClientAPI.RegisterClientVariable(Page, "oc_websiteRoot", HostingEnvironment.ApplicationVirtualPath, true);
            if (System.IO.File.Exists(Server.MapPath("~/Providers/HtmlEditorProviders/CKEditor/ckeditor.js")))
            {
                ClientResourceManager.RegisterScript(Page, "~/Providers/HtmlEditorProviders/CKEditor/ckeditor.js", FileOrder.Js.DefaultPriority);
                DotNetNuke.UI.Utilities.ClientAPI.RegisterClientVariable(Page, "PortalId", PortalId.ToString(), true);
                CKDNNporid.Value = PortalId.ToString();
            }
            var folderId = Convert.ToInt32(Request.Params["FolderId"]);
            Folder = FolderManager.Instance.GetFolder(folderId);

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Page.IsPostBack)
            {

            }
        }

        public void PrepareProperties()
        {
            //base.PrepareProperties();
            //TitleInput.Text = File.Title;
        }
        public object SaveProperties()
        {
            //var file = base.SaveProperties();
            //File.Title = TitleInput.Text;
            //ContentItem item;
            //if (File.ContentItemID == Null.NullInteger)
            //{
            //    item = CreateFileContentItem();
            //    File.ContentItemID = item.ContentItemId;
            //}
            //else
            //{
            //    item = Util.GetContentController().GetContentItem(File.ContentItemID);
            //}
            //item.Content = hfAlpacaData.Value;
            //Util.GetContentController().UpdateContentItem(item);


            //FileManager.Instance.UpdateFile(File);

            //IFolderInfo Folder = FolderManager.Instance.GetFolder(_Item.ItemID);
            return Folder;
        }

        private ContentItem CreateFileContentItem()
        {
            var typeController = new ContentTypeController();
            var contentTypeFile = (from t in typeController.GetContentTypes() where t.ContentType == "File" select t).SingleOrDefault();

            if (contentTypeFile == null)
            {
                contentTypeFile = new ContentType { ContentType = "File" };
                contentTypeFile.ContentTypeId = typeController.AddContentType(contentTypeFile);
            }

            var objContent = new ContentItem
            {
                ContentTypeId = contentTypeFile.ContentTypeId,
                Indexed = false,
            };

            objContent.ContentItemId = Util.GetContentController().AddContentItem(objContent);

            return objContent;
        }

        public int ContentItemId
        {
            get
            {
                return -1;//File.ContentItemID;

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

        IDigitalAssetsController _Controller;

        public IDigitalAssetsController Controller
        {
            get {
                return _Controller;
            }
        }

        ItemViewModel _Item;
        public ItemViewModel Item
        {
            get {
                return _Item;
            }
        }

        public void SetController(IDigitalAssetsController damController)
        {
            _Controller = damController;
        }

        public void SetItemViewModel(ItemViewModel itemViewModel)
        {
            _Item = itemViewModel;
        }

        bool _availability;
        public void SetPropertiesAvailability(bool availability)
        {
            _availability = availability;
        }

        bool _visibility;
        public void SetPropertiesVisibility(bool visibility)
        {
            _visibility = visibility;
        }
    }
}