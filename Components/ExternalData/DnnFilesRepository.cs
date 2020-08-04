#region Usings

using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenFiles.Components.Lucene;
using Satrabel.OpenFiles.Components.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace Satrabel.OpenFiles.Components.ExternalData
{
    public class DnnFilesRepository
    {
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Returns the collection of LuceneIndexItem ready to be indexed by Lucene.
        /// </summary>
        /// -----------------------------------------------------------------------------
        public IEnumerable<LuceneIndexItem> GetPortalSearchDocuments(int portalId, string folderPath, bool recursive, DateTime? startDateLocal)
        {
            var searchDocuments = new List<LuceneIndexItem>();
            var folderManager = FolderManager.Instance;
            var folder = folderManager.GetFolder(portalId, folderPath);

            try
            {
                var files = folderManager.GetFiles(folder, recursive);
                if (startDateLocal.HasValue)
                {
                    files = files.Where(f => f.LastModifiedOnDate > startDateLocal.Value);
                }

                var indexConfig = FilesRepository.GetIndexConfig(portalId);
                foreach (var file in files)
                {
                    var indexData = LuceneMappingUtils.CreateLuceneItem(file, indexConfig);
                    searchDocuments.Add(indexData);
                }
            }
            catch (Exception ex)
            {
                Exceptions.LogException(new Exception($"Error in GetPortalSearchDocuments for portal {portalId}", ex));
            }

            return searchDocuments;
        }

        internal static string GetFileContent(IFileInfo file)
        {
            string filename = file == null ? "unknown filename. IFileInfo is null." : file.FileName;
            try
            {
                string extension = Path.GetExtension(file.FileName);

                if (extension == ".pdf")
                {
                    var fileContent = FileManager.Instance.GetFileContent(file);
                    if (fileContent != null)
                    {
                        Log.Logger.Debug($"Indexing file [{filename}].");
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
                            Log.Logger.Debug($"Indexing file [{filename}].");
                            return reader.ReadToEnd();
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