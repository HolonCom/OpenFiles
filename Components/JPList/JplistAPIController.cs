using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Security;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Web.Api;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenFiles.Components.Lucene;
using Satrabel.OpenFiles.Components.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Security.Permissions;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.TemplateHelpers;
using Satrabel.OpenContent.Components.Datasource.search;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenContent.Components.JPList;

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
                Log.Logger.DebugFormat("OpenFiles.JplistApiController.List() called with request [{0}].", req.ToJson());

                QueryBuilder queryBuilder = new QueryBuilder();
                queryBuilder.BuildFilter(PortalSettings.PortalId, req.folder);
                JplistQueryBuilder.MergeJpListQuery(queryBuilder.Select, req.StatusLst);

                string curFolder = NormalizePath(req.folder);
                foreach (var item in queryBuilder.Select.Query.FilterRules.Where(f => f.Field == "Folder"))
                {
                    curFolder = NormalizePath(item.Value.AsString);
                    item.Value = new StringRuleValue(NormalizePath(item.Value.AsString)); //any file of current folder
                }

                var def = new SelectQueryDefinition();
                def.Build(queryBuilder.Select);

                var docs = LuceneController.Instance.Search(def);
                int total = docs.TotalResults;

                var ratio = string.IsNullOrEmpty(req.imageRatio) ? new Ratio(100, 100) : new Ratio(req.imageRatio);

                Log.Logger.DebugFormat("OpenFiles.JplistApiController.List() Searched for [{0}], found [{1}] items", def.Filter.ToJson() + " / " + def.Query.ToJson(), total);

                var fileManager = FileManager.Instance;
                var data = new List<FileDTO>();
                var breadcrumbs = new List<IFolderInfo>();
                if (req.withSubFolder)
                {
                    breadcrumbs = AddFolders(NormalizePath(req.folder), curFolder, fileManager, data, ratio);
                }

                foreach (var doc in docs.ids)
                {
                    IFileInfo f = fileManager.GetFile(doc.FileId);
                    if (f == null)
                    {
                        //file seems to have been deleted
                        LuceneController.Instance.Delete(doc.FileId, doc.PortalId);
                        total -= 1;
                    }
                    else
                    {
                        if (f.FileName == "_folder.jpg")
                        {
                            continue; // skip
                        }
                        var custom = GetCustomFileDataAsDynamic(f);
                        dynamic title = null;
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

                        data.Add(new FileDTO()
                        {
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
                            IsEditable = IsEditable,
                            EditUrl = IsEditable ? GetFileEditUrl(f) : ""
                        });
                    }
                }

                //Sort as requested
                //data = SortAsRequested(data, jpListQuery);

                //Page as requested
                //if (jpListQuery.Pagination.number > 0)
                //    data = data.Skip((jpListQuery.Pagination.currentPage) * jpListQuery.Pagination.number).Take(jpListQuery.Pagination.number).ToList();

                if (req.withSubFolder)
                {
                    var res = new ResultExtDTO<FileDTO>()
                    {
                        data = new ResultDataDTO<FileDTO>()
                        {
                            items = data,
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
                else
                {
                    var res = new ResultDTO<FileDTO>()
                    {
                        data = data,
                        count = total
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, res);
                }

            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
        private List<IFolderInfo> AddFolders(string baseFolder, string curFolder, IFileManager fileManager, List<FileDTO> data, Ratio ratio)
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
                var files = folderManager.GetFiles(f, false);
                var firstFile = files.FirstOrDefault(fi => fi.FileName == "_folder.jpg");
                if (firstFile == null)
                {
                    firstFile = files.OrderBy(fi => fi.FileName).FirstOrDefault();
                    //firstFile = folderManager.GetFiles(f, true).OrderBy(fi => fi.FileName).FirstOrDefault();
                }
                if (firstFile != null)
                {
                    var custom = GetCustomFileDataAsDynamic(firstFile);
                    //dynamic title = null;
                    //if (custom != null && custom.meta != null)
                    //{
                    //    try
                    //    {
                    //        title = Normalize.DynamicValue(custom.meta.title, "");
                    //    }
                    //    catch (Exception)
                    //    {
                    //    }
                    //}
                    dto.FileName = firstFile.FileName;
                    dto.Url = fileManager.GetUrl(firstFile);

                    dto.IsImage = fileManager.IsImageFile(firstFile);
                    dto.ImageUrl = ImageHelper.GetImageUrl(firstFile, ratio);
                    dto.Custom = custom;
                    dto.IconUrl = GetFileIconUrl(firstFile.Extension);
                    dto.IsEditable = IsEditable;
                    dto.EditUrl = IsEditable ? GetFileEditUrl(firstFile) : "";
                }
            }
            var path = new List<IFolderInfo>();
            path.Add(folder);
            while (folder.ParentID > 0)
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

        private List<FileDTO> SortAsRequested(List<FileDTO> data, JpListQueryDTO jpListQuery)
        {
            //This implementation is not more than a hack for one project.
            //todo add support for multiple sorting field
            //todo add support for other sorting fields
            //todo refactor to using Func<> to support more flexible approach

            if (data.Count == 0) return data;

            List<FileDTO> newdata = null;
            foreach (var sort in jpListQuery.Sorts)
            {
                if (string.Equals(sort.path, "LastModifiedOnDate", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (string.Equals(sort.order, "desc", StringComparison.InvariantCultureIgnoreCase))
                        newdata = data.OrderByDescending(i => i.LastModifiedOnDate).ToList();
                    else
                        newdata = data.OrderBy(i => i.LastModifiedOnDate).ToList();
                }
                else if (string.Equals(sort.path, "Name", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (string.Equals(sort.order, "desc", StringComparison.InvariantCultureIgnoreCase))
                        newdata = data.OrderByDescending(i => i.Name).ToList();
                    else
                        newdata = data.OrderBy(i => i.Name).ToList();
                }
                else if (string.Equals(sort.path, "FileName", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (string.Equals(sort.order, "desc", StringComparison.InvariantCultureIgnoreCase))
                        newdata = data.OrderByDescending(i => i.FileName).ToList();
                    else
                        newdata = data.OrderBy(i => i.FileName).ToList();
                }
                else if (string.Equals(sort.path, "Description", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (string.Equals(sort.order, "desc", StringComparison.InvariantCultureIgnoreCase))
                        newdata = data.OrderByDescending(i => i.Custom.Description).ToList();
                    else
                        newdata = data.OrderBy(i => i.FileName).ToList();
                }

            }
            return newdata ?? data;
        }

        private bool? _isEditable;
        private bool IsEditable
        {
            get
            {
                //Perform tri-state switch check to avoid having to perform a security
                //role lookup on every property access (instead caching the result)
                if (!_isEditable.HasValue)
                {
                    bool blnPreview = (PortalSettings.UserMode == PortalSettings.Mode.View);
                    if (Globals.IsHostTab(PortalSettings.ActiveTab.TabID))
                    {
                        blnPreview = false;
                    }
                    bool blnHasModuleEditPermissions = false;
                    if (ActiveModule != null)
                    {
                        blnHasModuleEditPermissions = ModulePermissionController.HasModuleAccess(SecurityAccessLevel.Edit, "CONTENT", ActiveModule);
                    }
                    if (blnPreview == false && blnHasModuleEditPermissions)
                    {
                        _isEditable = true;
                    }
                    else
                    {
                        _isEditable = false;
                    }
                }
                return _isEditable.Value;
            }
        }
        private string NormalizePath(string filePath)
        {
            filePath = filePath.Replace("\\", "/");
            filePath = filePath.Trim('~');
            //filePath = filePath.TrimStart(NormalizedApplicationPath);
            filePath = filePath.Trim('/');
            return filePath;
        }

        private string GetFileEditUrl(IFileInfo f)
        {
            if (f == null) return "";
            var portalFileUri = new PortalFileUri(f);
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

        private dynamic GetCustomFileDataAsDynamic(IFileInfo f)
        {
            if (f.ContentItemID > 0)
            {
                var item = Util.GetContentController().GetContentItem(f.ContentItemID);
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
