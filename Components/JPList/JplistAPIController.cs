using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Web.Api;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenDocument.Components.Lucene;
using Satrabel.OpenDocument.Components.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Satrabel.OpenContent.Components.TemplateHelpers;
using TemplateHelper = Satrabel.OpenDocument.Components.Template.TemplateHelper;

namespace Satrabel.OpenDocument.Components.JPList
{
    //[SupportedModules("OpenDocument")]
    public class JplistAPIController : DnnApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(OpenDocumentAPIController));

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
                if (jpListQuery.Pagination.currentPage > 0)
                    docs = docs.Skip(jpListQuery.Pagination.currentPage * jpListQuery.Pagination.number).Take(jpListQuery.Pagination.number);
                var fileManager = FileManager.Instance;
                var data = new List<FileDTO>();
                foreach (var doc in docs)
                {
                    var f = fileManager.GetFile(doc.FileId);
                    if (f == null)
                    {
                        //file seems to have been deleted
                        SearchEngine.RemoveDocument(doc.FileId);
                        total -= 1;
                    }
                    else
                    {
                        data.Add(new FileDTO()
                        {
                            FileName = f.FileName,
                            FolderName = f.Folder,
                            Url = fileManager.GetUrl(f),
                            ImageUrl = ImageHelper.GetImageUrl(f, new Ratio(100, 100)),
                            Custom = GetCustomFileDataAsDynamic(f),
                            IsImage = fileManager.IsImageFile(f),
                            IconUrl = GetFileIconUrl(f.Extension)
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
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
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

        #region Private Methods

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
