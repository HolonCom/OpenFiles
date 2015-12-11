using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Instrumentation;
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
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Http;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Security.Permissions;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.TemplateHelpers;
using TemplateHelper = Satrabel.OpenFiles.Components.Template.TemplateHelper;

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
                IEnumerable<LuceneIndexItem> docs;

                var jpListQuery = BuildJpListQuery(req.StatusLst);
                if (!string.IsNullOrEmpty(req.folder) && jpListQuery.Filters.All(f => f.name != "Folder")) // If there is no "Folder" filter active, then add one
                {
                    jpListQuery.Filters.Add(new FilterDTO()
                    {
                        name = "Folder",
                        value = NormalizePath(req.folder)
                    });
                }
                else
                {
                    foreach (var item in jpListQuery.Filters.Where(f => f.name == "Folder"))
                    {
                        item.path = NormalizePath(item.path);
                    }
                }
                jpListQuery.Filters.Add(new FilterDTO()
                {
                    name = "PortalId",
                    path = PortalSettings.PortalId.ToString()
                });

                string luceneQuery = BuildLuceneQuery(jpListQuery);
                if (string.IsNullOrEmpty(luceneQuery))
                {
                    docs = SearchEngine.GetAllIndexedRecords();
                }
                else
                {
                    docs = SearchEngine.Search(luceneQuery);
                }

                int total = docs.Count();
                if (jpListQuery.Pagination.number > 0)
                    docs = docs.Skip((jpListQuery.Pagination.currentPage) * jpListQuery.Pagination.number).Take(jpListQuery.Pagination.number);
                var fileManager = FileManager.Instance;
                var data = new List<FileDTO>();
                foreach (var doc in docs)
                {
                    IFileInfo f = fileManager.GetFile(doc.FileId);
                    if (f == null)
                    {
                        //file seems to have been deleted
                        SearchEngine.RemoveDocument(doc.FileId);
                        total -= 1;
                    }
                    else
                    {
                        var custom = GetCustomFileDataAsDynamic(f);
                        dynamic title = null;
                        if (custom != null && custom.meta != null)
                        {
                            title = Normalize.DynamicValue(custom.meta.title, "");
                        }
                        data.Add(new FileDTO()
                        {
                            Name = Normalize.DynamicValue(title, f.FileName),
                            FileName = f.FileName,
                            FolderName = f.Folder,
                            Url = fileManager.GetUrl(f),
                            IsImage = fileManager.IsImageFile(f),
                            ImageUrl = ImageHelper.GetImageUrl(f, new Ratio(100, 100)),
                            Custom = custom,
                            IconUrl = GetFileIconUrl(f.Extension),
                            IsEditable = IsEditable,
                            EditUrl = IsEditable ? GetFileEditUrl(f) : ""
                        });
                    }
                }

                var res = new ResultDTO<FileDTO>()
                {
                    data = data,
                    count = total
                };

                return Request.CreateResponse(HttpStatusCode.OK, res);
            }
            catch (Exception exc)
            {
                Utils.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }

        }


        #region Private Methods

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

        private JpListQueryDTO BuildJpListQuery(List<StatusDTO> statuses)
        {
            var query = new JpListQueryDTO();
            foreach (StatusDTO status in statuses)
            {
                switch (status.action)
                {
                    case "paging":
                        {
                            int number = 100000;
                            //  string value (it could be number or "all")
                            int.TryParse(status.data.number, out number);
                            query.Pagination = new PaginationDTO()
                            {
                                number = number,
                                currentPage = status.data.currentPage
                            };
                            break;
                        }

                    case "filter":
                        {
                            if (status.type == "textbox" && status.data != null && !String.IsNullOrEmpty(status.name) && !String.IsNullOrEmpty(status.data.value))
                            {
                                query.Filters.Add(new FilterDTO()
                                {
                                    name = status.name,
                                    value = status.data.value,
                                    pathGroup = status.data.pathGroup

                                });
                            }

                            else if (status.type == "checkbox-group-filter" && status.data != null && !String.IsNullOrEmpty(status.name))
                            {
                                if (status.data.filterType == "pathGroup" && status.data.pathGroup != null && status.data.pathGroup.Count > 0)
                                {
                                    foreach (var path in status.data.pathGroup)
                                    {
                                        query.Filters.Add(new FilterDTO()
                                        {
                                            name = status.name,
                                            value = status.data.value,
                                            path = status.data.path,
                                            pathGroup = status.data.pathGroup

                                        });
                                    }
                                }
                            }
                            else if (status.type == "filter-select" && status.data != null && !String.IsNullOrEmpty(status.name))
                            {
                                if (status.data.filterType == "path" && status.data.path != null)
                                {
                                    query.Filters.Add(new FilterDTO()
                                    {
                                        name = status.name,
                                        path = status.data.path,
                                    });
                                }
                            }
                            break;
                        }

                    case "sort":
                        {
                            query.Sorts.Add(new SortDTO()
                            {
                                path = status.data.path,
                                order = status.data.order
                            });
                            break;
                        }
                }

            }
            return query;
        }

        private string BuildLuceneQuery(JpListQueryDTO jpListQuery)
        {

            string queryStr = "";
            if (jpListQuery.Filters.Any())
            {
                foreach (FilterDTO f in jpListQuery.Filters)
                {
                    if (f.pathGroup != null && f.pathGroup.Any()) //group is bv multicheckbox, vb categories where(categy="" OR category="")
                    {
                        string pathStr = "";
                        foreach (var p in f.pathGroup)
                        {
                            pathStr += (string.IsNullOrEmpty(pathStr) ? "" : " OR ") + f.name + ":" + p;
                        }

                        queryStr += "+" + "(" + pathStr + ")";
                    }
                    else
                    {
                        string[] names = f.name.Split(',');
                        string pathStr = "";
                        foreach (var n in names)
                        {
                            if (!string.IsNullOrEmpty(f.path))
                            {
                                pathStr += (string.IsNullOrEmpty(pathStr) ? "" : " OR ") + n + ":" + f.path;  //for dropdownlists; value is keyword => never partial search
                            }
                            else
                            {
                                pathStr += (string.IsNullOrEmpty(pathStr) ? "" : " OR ") + n + ":" + f.value + "*";   //textbox
                            }
                        }
                        queryStr += "+" + "(" + pathStr + ")";
                    }
                }
            }
            return queryStr;
        }

        #endregion
    }
}
