using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Security;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Web.Api;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenFiles.Components.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.WebPages;
using DotNetNuke.Entities.Portals;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Datasource.Search;
using Satrabel.OpenContent.Components.Dnn;
using Satrabel.OpenContent.Components.JPList;
using Satrabel.OpenContent.Components.Lucene;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenContent.Components.TemplateHelpers;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Utils;
using LuceneController = Satrabel.OpenFiles.Components.Lucene.LuceneController;

namespace Satrabel.OpenFiles.Components.JPList
{
    //[SupportedModules("OpenFiles")]
    public class JplistAPIController : DnnApiController
    {
        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
        [HttpPost]
        public HttpResponseMessage List(RequestDTO req)
        {
            try
            {
                Log.Logger.Debug($"OpenFiles.JplistApiController.List() called with request [{req.ToJson()}].");

                var module = OpenContentModuleConfig.Create(ActiveModule, PortalSettings);
                FieldConfig indexConfig = FilesRepository.GetIndexConfig(PortalSettings.PortalId);
                bool addWorkFlow = PortalSettings.UserMode != PortalSettings.Mode.Edit;
                QueryBuilder queryBuilder = new QueryBuilder(indexConfig);
                queryBuilder.BuildFilter(PortalSettings.PortalId, req.folder, addWorkFlow, PortalSettings.UserInfo.Social.Roles);
                JplistQueryBuilder.MergeJpListQuery(indexConfig, queryBuilder.Select, req.StatusLst, DnnLanguageUtils.GetCurrentCultureCode());

                string curFolder = NormalizePath(req.folder);
                foreach (var item in queryBuilder.Select.Query.FilterRules.Where(f => f.Field == LuceneMappingUtils.FOLDER_FIELD))
                {
                    curFolder = NormalizePath(item.Value.AsString);
                    item.Value = new StringRuleValue(NormalizePath(item.Value.AsString)); //any file of current folder
                }

                var def = new SelectQueryDefinition();
                def.Build(queryBuilder.Select);

                var docs = LuceneController.Instance.Search(def);
                int total = docs.TotalResults;

                var ratio = string.IsNullOrEmpty(req.imageRatio) ? new Ratio(100, 100) : new Ratio(req.imageRatio);

                Log.Logger.Debug($"OpenFiles.JplistApiController.List() Searched for [{def.Filter} / {def.Query}], found [{total}] items");

                //if (LogContext.IsLogActive)
                //{
                //    var logKey = "Query";
                //    LogContext.Log(ActiveModule.ModuleID, logKey, "select", queryBuilder.Select);
                //    LogContext.Log(ActiveModule.ModuleID, logKey, "debuginfo", dsItems.DebugInfo);
                //    LogContext.Log(ActiveModule.ModuleID, logKey, "model", model);
                //    model["Logs"] = JToken.FromObject(LogContext.Current.ModuleLogs(ActiveModule.ModuleID));
                //}

                var fileManager = FileManager.Instance;
                var retval = new List<FileDTO>();
                var breadcrumbs = new List<IFolderInfo>();
                if (req.withSubFolder)
                {
                    //adding results here that do not come from Lucene
                    breadcrumbs = AddFolders(module, NormalizePath(req.folder), curFolder, fileManager, retval, ratio);
                }

                //reset retval again if we are doing a textsearch, because we don't want to include the folders
                if (req.StatusLst.Any(i => i.action == "filter" && i.data.filterType == "TextFilter" && !i.data.value.IsEmpty()))
                {
                    retval = new List<FileDTO>();
                }
                foreach (var doc in docs.ids)
                {
                    IFileInfo f = fileManager.GetFile(doc.FileId);
                    if (f == null)
                    {
                        //file seems to have been deleted
                        LuceneController.Instance.Delete(LuceneMappingUtils.CreateLuceneItem(doc.PortalId, doc.FileId));
                        total -= 1;
                    }
                    else
                    {
                        if (f.FileName == "_folder.jpg")
                        {
                            continue; // skip
                        }
                        dynamic title = null;
                        var custom = GetCustomFileDataAsDynamic(f);
                        if (custom != null && custom.meta != null)
                        {
                            try
                            {
                                title = Normalize.DynamicValue(custom.meta.title, "");
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Debug("OpenFiles.JplistApiController.List() Failed to get title.", ex);
                            }
                        }

                        retval.Add(new FileDTO()
                        {
                            Id = f.FileId,
                            Name = Normalize.DynamicValue(title, f.FileName),
                            FileName = f.FileName,
                            CreatedOnDate = f.CreatedOnDate,
                            LastModifiedOnDate = f.LastModifiedOnDate,
                            FolderName = f.Folder,
                            Url = fileManager.GetUrl(f),
                            IsImage = fileManager.IsImageFile(f),
                            ImageUrl = ImageHelper.GetImageUrl(f, ratio),
                            Custom = custom,
                            IconUrl = GetFileIconUrl(f.Extension),
                            IsEditable = IsEditable(module),
                            EditUrl = IsEditable(module) ? GetFileEditUrl(f) : ""
                        });
                    }
                }

                var res = new ResultExtDTO<FileDTO>()
                {
                    data = new ResultDataDTO<FileDTO>()
                    {
                        items = retval,
                        breadcrumbs = breadcrumbs.Select(f => new ResultBreadcrumbDTO
                        {
                            name = f.FolderName,
                            path = f.FolderPath.Trim('/')
                        })
                    },
                    count = total
                };
                return Request.CreateResponse(HttpStatusCode.OK, res);

            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
        private List<IFolderInfo> AddFolders(OpenContentModuleConfig module, string baseFolder, string curFolder, IFileManager fileManager, List<FileDTO> data, Ratio ratio)
        {
            var folderManager = FolderManager.Instance;
            var folder = folderManager.GetFolder(PortalSettings.PortalId, curFolder);
            var folders = folderManager.GetFolders(folder);
            foreach (var f in folders)
            {
                var dto = new FileDTO()
                {
                    Name = f.DisplayName,
                    CreatedOnDate = f.CreatedOnDate,
                    LastModifiedOnDate = f.LastModifiedOnDate,
                    FolderName = f.FolderName,
                    FolderPath = f.FolderPath.Trim('/'),
                    IsFolder = true
                };
                data.Add(dto);
                var files = folderManager.GetFiles(f, false).ToList();
                var firstFile = files.FirstOrDefault(fi => fi.FileName == "_folder.jpg");
                if (firstFile == null)
                {
                    firstFile = files.OrderBy(fi => fi.FileName).FirstOrDefault();
                }
                if (firstFile != null && fileManager.IsImageFile(firstFile))
                {
                    var custom = GetCustomFileDataAsDynamic(firstFile);

                    dto.FileName = firstFile.FileName;
                    dto.Url = fileManager.GetUrl(firstFile);
                    dto.IsImage = fileManager.IsImageFile(firstFile);
                    dto.ImageUrl = dto.IsImage ? ImageHelper.GetImageUrl(firstFile, ratio) : "";
                    dto.Custom = custom;
                    dto.IconUrl = GetFileIconUrl(firstFile.Extension);
                    dto.IsEditable = IsEditable(module);
                    dto.EditUrl = IsEditable(module) ? GetFileEditUrl(firstFile) : "";
                }
                else
                {
                    dto.FileName = f.FolderName;
                    dto.IsImage = false;
                    dto.IconUrl = GetFolderIconUrl();
                    dto.IsEditable = false;
                    dto.EditUrl = "";
                }
            }
            var path = new List<IFolderInfo>();
            path.Add(folder);
            while (folder.ParentID > 0 && NormalizePath(folder.FolderPath) != baseFolder)
            {
                folder = folderManager.GetFolder(folder.ParentID);
                path.Insert(0, folder);
                if (string.IsNullOrEmpty(folder.FolderPath) || NormalizePath(folder.FolderPath) == baseFolder)
                {
                    break;
                }
            }
            return path;
        }

        #region Private Methods

        private bool? _isEditable;
        private bool IsEditable(OpenContentModuleConfig module)
        {
            //Perform tri-state switch check to avoid having to perform a security
            //role lookup on every property access (instead caching the result)
            if (!_isEditable.HasValue)
            {
                _isEditable = module.ViewModule.CheckIfEditable(module);
            }
            return _isEditable.Value;
        }

        private static string NormalizePath(string filePath)
        {
            filePath = filePath.Replace("\\", "/");
            filePath = filePath.Trim('~');
            //filePath = filePath.TrimStart(NormalizedApplicationPath);
            filePath = filePath.Trim('/');
            return filePath;
        }

        private static string GetFileEditUrl(IFileInfo fileInfo)
        {
            if (fileInfo == null) return "";
            var portalFileUri = new PortalFileUri(fileInfo);
            return portalFileUri.EditUrl();
        }

        private static string GetFileIconUrl(string extension)
        {
            if (!string.IsNullOrEmpty(extension) && File.Exists(HttpContext.Current.Server.MapPath(IconController.IconURL("Ext" + extension, "32x32", "Standard"))))
            {
                return IconController.IconURL("Ext" + extension, "32x32", "Standard");
            }

            return IconController.IconURL("ExtFile", "32x32", "Standard");
        }
        private static string GetFolderIconUrl()
        {
            return IconController.IconURL("FolderStandard", "32x32", "Standard");
        }
        private static dynamic GetCustomFileDataAsDynamic(IFileInfo f)
        {
            if (f.ContentItemID > 0)
            {
                var item = Util.GetContentController().GetContentItem(f.ContentItemID);
                if (item == null)
                {
                    Log.Logger.ErrorFormat("Could not find contentitem for {0},{1},{2},{3},", f.FileId, f.Folder, f.FileName, f.ContentItemID);
                    return new JObject();
                }
                return JsonUtils.JsonToDynamic(item.Content);
            }
            else
            {
                return new JObject();
            }
        }

        #endregion
    }
}
