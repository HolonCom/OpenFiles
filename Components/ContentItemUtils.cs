using System;
using System.Linq;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenFiles.Components.Lucene;

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

            FileManager.Instance.UpdateFile(file);
        }

        //public static void Convert(ModuleInfo module)
        //{
        //    var folderManager = FolderManager.Instance;
        //    var folder = folderManager.GetFolder(module.PortalID, "");
        //    var files = folderManager.GetFiles(folder, true);
        //    try
        //    {
        //        foreach (IFileInfo file in files)
        //        {
        //            var indexData = new LuceneIndexItem()
        //            {
        //                //PortalId = module.PortalID,
        //                //FileId = file.FileId,
        //                //FileName = file.FileName,
        //                //Folder = file.Folder.TrimEnd('/'),
        //                //Title = "",
        //                //Description = "",
        //                //FileContent = FileRepository.GetFileContent(file.FileName, file)
        //            };
        //            JObject custom = FileRepository.GetCustomFileData(file);
        //            if (custom["meta"] != null && custom["meta"].HasValues)
        //            {
        //                if (custom["meta"]["title"] != null)
        //                    indexData.Title = custom["meta"]["title"].ToString();
        //                if (custom["meta"]["description"] != null)
        //                    indexData.Description = custom["meta"]["description"].ToString();
        //                if (custom["meta"]["publicationdate"] != null)
        //                    indexData.PublicationDate = DateTime.Parse(custom["meta"]["publicationdate"].ToString());
        //                if (custom["meta"]["category"] is JArray)
        //                    foreach (JToken item in (custom["meta"]["category"] as JArray))
        //                    {
        //                        indexData.Categories.Add(item.ToString());
        //                    }
        //            }
        //            if (!string.IsNullOrEmpty(indexData.Description))
        //            {
        //                DateTime date;
        //                if (DateTime.TryParse(indexData.Description, out date))
        //                {
        //                    indexData.PublicationDate = date;
        //                    custom["meta"]["publicationdate"] = date.ToString("s");
        //                    string json = custom.ToString();
        //                    ContentItemUtils.Save(file, "meta", json);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Exceptions.LogException(ex);
        //    }
        //}

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