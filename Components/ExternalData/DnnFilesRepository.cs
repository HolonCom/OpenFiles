#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenFiles.Components.Lucene;

#endregion

namespace Satrabel.OpenFiles.Components.ExternalData
{
    public class DnnFilesRepository
    {
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Returns the collection of SearchDocuments populated with Tab MetaData for the given portal.
        /// </summary>
        /// <param name="portalId"></param>
        /// <param name="startDateLocal"></param>
        /// <returns></returns>
        /// <history>
        ///     [vnguyen]   04/16/2013  created
        /// </history>
        /// -----------------------------------------------------------------------------
        public IEnumerable<LuceneIndexItem> GetPortalSearchDocuments(int portalId, DateTime? startDateLocal)
        {
            var searchDocuments = new List<LuceneIndexItem>();
            var folderManager = FolderManager.Instance;
            var folder = folderManager.GetFolder(portalId, "");
            var files = folderManager.GetFiles(folder, true);
            if (startDateLocal.HasValue)
            {
                files = files.Where(f => f.LastModifiedOnDate > startDateLocal.Value);
            }
            try
            {
                foreach (var file in files)
                {
                    var indexData = new LuceneIndexItem()
                    {
                        PortalId = portalId,
                        FileId = file.FileId,
                        FileName = file.FileName,
                        Folder = file.Folder.TrimEnd('/'),
                        //Title = "",
                        //Description = "",
                        FileContent = GetFileContent(file.FileName, file)
                    };

                    JObject custom = GetCustomFileDataAsJObject(file);
                    if (custom["meta"] != null && custom["meta"].HasValues)
                    {
                        indexData.Meta = custom["meta"].ToString();
                        //if (custom["meta"]["title"] != null)
                        //    indexData.Title = custom["meta"]["title"].ToString();
                        //if (custom["meta"]["description"] != null)
                        //    indexData.Description = custom["meta"]["description"].ToString();
                        //if (custom["meta"]["publicationdate"] != null)
                        //    indexData.PublicationDate = DateTime.Parse(custom["meta"]["publicationdate"].ToString());
                        //if (custom["meta"]["category"] is JArray)
                        //    foreach (JToken item in (custom["meta"]["category"] as JArray))
                        //    {
                        //        indexData.Categories.Add(item.ToString());
                        //    }
                    }
                    searchDocuments.Add(indexData);
                }
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }

            return searchDocuments;
        }

        private static string GetFileContent(string filename, IFileInfo file)
        {
            try
            {
                string extension = Path.GetExtension(filename);
                if (extension == ".pdf")
                {
                    if (File.Exists(file.PhysicalPath))
                    {
                        var fileContent = FileManager.Instance.GetFileContent(file);
                        if (fileContent != null)
                        {
                            return PdfParser.ReadPdfFile(fileContent);
                        }
                    }
                }
                else if (extension == ".txt")
                {
                    if (File.Exists(file.PhysicalPath))
                    {
                        var fileContent = FileManager.Instance.GetFileContent(file);
                        if (fileContent != null)
                        {
                            using (var reader = new StreamReader(fileContent, Encoding.UTF8))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.WarnFormat("Ignoring file [{0}]. Failed reading content. Error: {1}", filename, ex.Message);
            }
            return "";
        }

        private static JObject GetCustomFileDataAsJObject(IFileInfo f)
        {
            if (f.ContentItemID > 0)
            {
                try
                {
                    var item = Util.GetContentController().GetContentItem(f.ContentItemID);
                    return JObject.Parse(item.Content);
                }
                catch (Exception ex)
                {
                    Exceptions.LogException(ex);
                }
            }
            return new JObject();
        }

        private static string GetCustomFileData(IFileInfo f)
        {
            if (f.ContentItemID > 0)
            {
                try
                {
                    var item = Util.GetContentController().GetContentItem(f.ContentItemID);
                    return item.Content;
                }
                catch (Exception ex)
                {
                    Exceptions.LogException(ex);
                }
            }
            return "";
        }
    }
}