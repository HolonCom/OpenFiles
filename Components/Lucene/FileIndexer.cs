#region Usings

using System;
using System.Linq;
using System.Collections.Generic;
using DotNetNuke.Common;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Search.Entities;
using DotNetNuke.Services.Search.Internals;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Entities.Content.Common;
using Satrabel.OpenContent.Components.Json;
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
                    var custom = GetCustomFileData(file);
                    var indexData = new LuceneIndexItem()
                    {
                        PortalId = portalId,
                        FileId = file.FileId,
                        FileName = file.FileName,
                        Folder = file.Folder.TrimEnd('/'),
                        Title = custom["Title"] == null ? "" : custom["Title"].ToString(),
                        Description = custom["Description"] == null ? "" : custom["Description"].ToString(),
                        FileContent = GetFileContent(file.FileName, file)
                    };
                    if (custom["Category"] != null)
                    {
                        foreach (dynamic item in custom["Category"])
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
                var fileContent = FileManager.Instance.GetFileContent(file);
                if (fileContent != null)
                {
                    return PdfParser.ReadPdfFile(fileContent);
                }
            }
            else if (extension == ".txt")
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