using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Satrabel.OpenFiles.Components.DigitalAssets
{
    public class ContentItemUtils
    {
        public static void Save(IFileInfo File, string Key, string Content){
            ContentItem item;
            if (File.ContentItemID == Null.NullInteger)
            {
                item = CreateFileContentItem();
                File.ContentItemID = item.ContentItemId;
            }
            else
            {
                item = Util.GetContentController().GetContentItem(File.ContentItemID);
            }
            JObject obj;
            if (string.IsNullOrEmpty(item.Content))
                obj = new JObject();
            else
                obj = JObject.Parse(item.Content);

            if (string.IsNullOrEmpty(Content))
                obj[Key] = new JObject();
            else
                obj[Key] = JObject.Parse(Content);

            item.Content = obj.ToString();            
            Util.GetContentController().UpdateContentItem(item);
            
            FileManager.Instance.UpdateFile(File);
        }

        private static ContentItem CreateFileContentItem()
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

    }
}