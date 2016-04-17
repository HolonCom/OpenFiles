#region Usings

using System;
using System.Linq;
using System.Collections.Generic;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Entities.Content.Common;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

#endregion

namespace Satrabel.OpenFiles.Components.Lucene
{
    public class FileIndexer
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
                        Title = "",
                        Description = "",
                        FileContent = GetFileContent(file.FileName, file)
                    };

                    var custom = GetCustomFileData(file);
                    if (custom["meta"] != null && custom["meta"].HasValues)
                    {
                        if (custom["meta"]["title"] != null)
                            indexData.Title = custom["meta"]["title"].ToString();
                        if (custom["meta"]["description"] != null)
                            indexData.Description = custom["meta"]["description"].ToString();
                        if (custom["meta"]["category"] != null)
                            foreach (dynamic item in custom["meta"]["category"])
                            {
                                indexData.Categories.Add(item);
                            }
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

        private string GetFileContent(string p, IFileInfo file)
        {
            string extension = Path.GetExtension(p);
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
            return "";
        }

        private static JObject GetCustomFileData(IFileInfo f)
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
    }
}