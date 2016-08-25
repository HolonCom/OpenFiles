using System.Linq;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenFiles.Components.Lucene;
using Satrabel.OpenFiles.Components.Lucene.Mapping;

namespace Satrabel.OpenFiles.Components
{
    public static class ContentItemUtils
    {
        public static void Save(IFileInfo file, string key, string content)
        {
            ContentItem item;
            if (file.ContentItemID == Null.NullInteger)
            {
                item = CreateFileContentItem();
                file.ContentItemID = item.ContentItemId;
            }
            else
            {
                item = Util.GetContentController().GetContentItem(file.ContentItemID);
            }
            JObject obj;
            obj = string.IsNullOrEmpty(item.Content) ? new JObject() : JObject.Parse(item.Content);

            if (string.IsNullOrEmpty(content))
                obj[key] = new JObject();
            else
                obj[key] = JObject.Parse(content);

            item.Content = obj.ToString();
            Util.GetContentController().UpdateContentItem(item);


            LuceneController.Instance.Update(LuceneMappingUtils.CreateLuceneItem(file));
            LuceneController.Instance.Store.Commit();

            FileManager.Instance.UpdateFile(file);
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