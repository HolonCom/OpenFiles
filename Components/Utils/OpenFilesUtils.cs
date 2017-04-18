using System;
using System.Linq;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Indexing;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Lucene;

namespace Satrabel.OpenFiles.Components.Utils
{
    public static class OpenFilesUtils
    {
        public static void Save(IFileInfo file, string key, string newContent)
        {

            ContentItem dnnContentItem;
            //if fileObject has already a ContentItem then load it, otherwise create new ContentItem
            if (file.ContentItemID == Null.NullInteger)
            {
                dnnContentItem = CreateDnnContentItem();
                file.ContentItemID = dnnContentItem.ContentItemId;
            }
            else
            {
                dnnContentItem = CreateDnnContentItem(file.ContentItemID);
            }
            JObject jsonContent = string.IsNullOrEmpty(dnnContentItem.Content) ? new JObject() : JObject.Parse(dnnContentItem.Content);

            if (string.IsNullOrEmpty(newContent))
                jsonContent[key] = new JObject();
            else
                jsonContent[key] = JObject.Parse(newContent);

            dnnContentItem.Content = jsonContent.ToString();
            Util.GetContentController().UpdateContentItem(dnnContentItem);


            //Save to lucene
            var item = new OpenFilesInfo(file, dnnContentItem, jsonContent);
            FieldConfig indexConfig = FilesRepository.GetIndexConfig(file.PortalId);

            LuceneController.Instance.Update(LuceneMappingUtils.CreateLuceneItem(item, indexConfig));
            LuceneController.Instance.Store.Commit();

            FileManager.Instance.UpdateFile(file);
        }

        internal static void HydrateDefaultFields(this JToken content, FieldConfig indexConfig)
        {
            if (indexConfig.HasField(AppConfig.FieldNamePublishStartDate) && !content.HasField(AppConfig.FieldNamePublishStartDate))
            {
                content[AppConfig.FieldNamePublishStartDate] = DateTime.MinValue;
            }
            if (indexConfig.HasField(AppConfig.FieldNamePublishEndDate) && !content.HasField(AppConfig.FieldNamePublishEndDate))
            {
                content[AppConfig.FieldNamePublishEndDate] = DateTime.MaxValue;
            }
            if (indexConfig.HasField(AppConfig.FieldNamePublishStatus) && !content.HasField(AppConfig.FieldNamePublishStatus))
            {
                content[AppConfig.FieldNamePublishStatus] = "published";
            }
        }

        private static ContentItem CreateDnnContentItem(int contentItemId)
        {
            return Util.GetContentController().GetContentItem(contentItemId);
        }

        private static ContentItem CreateDnnContentItem()
        {
            var typeController = new ContentTypeController();
            var contentTypeFile = (from t in typeController.GetContentTypes() where t.ContentType == "File" select t).SingleOrDefault();

            if (contentTypeFile == null)
            {
                contentTypeFile = new ContentType { ContentType = "File" };
                contentTypeFile.ContentTypeId = typeController.AddContentType(contentTypeFile);
            }

            var dnnContentItem = new ContentItem
            {
                ContentTypeId = contentTypeFile.ContentTypeId,
                Indexed = false,
            };

            dnnContentItem.ContentItemId = Util.GetContentController().AddContentItem(dnnContentItem);

            return dnnContentItem;
        }

    }
}