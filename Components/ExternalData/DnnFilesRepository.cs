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
using Satrabel.OpenContent.Components.Indexing;
using Satrabel.OpenFiles.Components.Lucene;
using Satrabel.OpenFiles.Components.Utils;

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
        /// <param name="folderPath"></param>
        /// <param name="recursive"></param>
        /// <param name="startDateLocal"></param>
        /// <returns></returns>
        /// <history>
        ///     [vnguyen]   04/16/2013  created
        /// </history>
        /// -----------------------------------------------------------------------------
        public IEnumerable<LuceneIndexItem> GetPortalSearchDocuments(int portalId, string folderPath, bool recursive, DateTime? startDateLocal)
        {
            var searchDocuments = new List<LuceneIndexItem>();
            var folderManager = FolderManager.Instance;
            var folder = folderManager.GetFolder(portalId, folderPath);
            var files = folderManager.GetFiles(folder, recursive);
            if (startDateLocal.HasValue)
            {
                files = files.Where(f => f.LastModifiedOnDate > startDateLocal.Value);
            }
            try
            {
                FieldConfig indexConfig = FilesRepository.GetIndexConfig(portalId);
                foreach (var file in files)
                {
                    var indexData = LuceneMappingUtils.CreateLuceneItem(file, indexConfig);
                    searchDocuments.Add(indexData);
                }
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }

            return searchDocuments;
        }

        internal static string GetFileContent(IFileInfo file)
        {
            try
            {
                string extension = Path.GetExtension(file.FileName);

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
            }
            catch (Exception ex)
            {
                string filename = file == null ? "unknown filename. IFileInfo is null." : file.FileName;
                Log.Logger.WarnFormat("Ignoring file [{0}]. Failed reading content. Error: {1}", filename, ex.Message);
            }
            return "";
        }

        internal static JObject GetCustomFileDataAsJObject(IFileInfo f)
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