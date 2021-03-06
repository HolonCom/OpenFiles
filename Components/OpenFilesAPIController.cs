#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetNuke.Web.Api;
using Newtonsoft.Json.Linq;
using System.Web.Hosting;
using System.IO;
using DotNetNuke.Security;
using DotNetNuke.Entities.Content.Common;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Utils;

#endregion

namespace Satrabel.OpenFiles.Components
{
    // [SupportedModules("OpenFiles")]
    public class OpenFilesAPIController : DnnApiController
    {
        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)] // rechten van FileManager module!!
        [HttpGet]
        public HttpResponseMessage Edit(int id)
        {
            try
            {
                JObject json = FilesRepository.GetSchemaAndOptionsJson(AppConfig.Instance.SchemaFolder, AppConfig.Instance.PortalFolder(PortalSettings.PortalId, PortalSettings.HomeDirectory), "");
                if (id > 0)
                {
                    var item = Util.GetContentController().GetContentItem(id);
                    if (!string.IsNullOrEmpty(item?.Content))
                    {
                        JObject dataJson = JObject.Parse(item.Content);
                        json["data"] = dataJson[LuceneMappingUtils.META_FIELD];
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        
        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage Settings(string Template)
        {
            string data = (string)ActiveModule.ModuleSettings["data"];
            try
            {
                string templateFilename = HostingEnvironment.MapPath("~/" + Template);
                string prefix = Path.GetFileNameWithoutExtension(templateFilename) + "-";

                JObject json = FilesRepository.GetSchemaAndOptionsJson(AppConfig.Instance.SchemaFolder, AppConfig.Instance.PortalFolder(PortalSettings.PortalId, PortalSettings.HomeDirectory), prefix);

                if (!string.IsNullOrEmpty(data))
                {
                    try
                    {
                        json["data"] = JObject.Parse(data);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error("Settings Json Data : " + data, ex);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public HttpResponseMessage Update(JObject json)
        {
            try
            {
                int moduleId = ActiveModule.ModuleID;
                //string Template = (string)ActiveModule.ModuleSettings["template"];

                return Request.CreateResponse(HttpStatusCode.OK, "");
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage EditImages(int id)
        {
            //string Template = "DesktopModules/OpenFiles/";
            try
            {
                JObject json = FilesRepository.GetSchemaAndOptionsJson(AppConfig.Instance.SchemaFolder, AppConfig.Instance.PortalFolder(PortalSettings.PortalId, PortalSettings.HomeDirectory), "images");

                //int moduleId = ActiveModule.ModuleID;
                if (id > 0)
                {
                    var item = Util.GetContentController().GetContentItem(id);
                    if (item != null && !string.IsNullOrEmpty(item.Content))
                    {
                        JObject dataJson = JObject.Parse(item.Content);
                        json["data"] = dataJson["crop"];
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
    }
}

