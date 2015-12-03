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
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using Satrabel.OpenContent.Components.Json;
using DotNetNuke.Entities.Content.Common;
using Satrabel.OpenContent.Components;

#endregion

namespace Satrabel.OpenFiles.Components
{
    // [SupportedModules("OpenFiles")]
    public class OpenFilesAPIController : DnnApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(OpenFilesAPIController));
        public string BaseDir
        {
            get
            {
                return PortalSettings.HomeDirectory + "/OpenFiles/Templates/";
            }
        }

        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage Edit(int id)
        {
            //string Template = "DesktopModules/OpenFiles/";
            JObject json = new JObject();
            try
            {
                string desktopFolder = HostingEnvironment.MapPath("~/DesktopModules/OpenFiles/");
                string portalFolder = HostingEnvironment.MapPath(PortalSettings.HomeDirectory + "/OpenFiles/");
                GetJson(json, desktopFolder, portalFolder, "");
                int moduleId = ActiveModule.ModuleID;
                if (id > 0)
                {
                    var item = Util.GetContentController().GetContentItem(id);
                    if (item != null && !string.IsNullOrEmpty(item.Content))
                    {
                        JObject dataJson = JObject.Parse(item.Content);
                        json["data"] = dataJson["meta"];
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        private void GetJson(JObject json, string desktopFolder, string portalFolder, string prefix)
        {
            if (!Directory.Exists(portalFolder))
            {
                Directory.CreateDirectory(portalFolder);
            }
            if (!string.IsNullOrEmpty(prefix))
            {
                prefix = prefix + "-";
            }
            // schema
            string schemaFilename = portalFolder + "\\" + prefix + "schema.json";
            if (!File.Exists(schemaFilename))
            {
                schemaFilename = desktopFolder + "\\" + prefix + "schema.json";
            }
            JObject schemaJson = JObject.Parse(File.ReadAllText(schemaFilename));
            json["schema"] = schemaJson;
            // default options
            string optionsFilename = portalFolder + "\\" + prefix + "options.json";
            if (!File.Exists(optionsFilename))
            {
                optionsFilename = desktopFolder + "\\" + prefix + "options.json";
            }
            if (File.Exists(optionsFilename))
            {
                string fileContent = File.ReadAllText(optionsFilename);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    JObject optionsJson = JObject.Parse(fileContent);
                    json["options"] = optionsJson;
                }
            }
            // language options
            optionsFilename = portalFolder + "\\" + prefix + "options." + DnnUtils.GetCurrentCultureCode() + ".json";
            if (!File.Exists(optionsFilename))
            {
                optionsFilename = desktopFolder + "\\" + prefix + "options." + DnnUtils.GetCurrentCultureCode() + ".json";
            }
            if (File.Exists(optionsFilename))
            {
                string fileContent = File.ReadAllText(optionsFilename);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    JObject optionsJson = JObject.Parse(fileContent);
                    json["options"] = json["options"].JsonMerge(optionsJson);
                }
            }
            // view
            /*
            string viewFilename = TemplateFolder + "\\" + Prefix +"view.json";
            if (File.Exists(optionsFilename))
            {
                string fileContent = File.ReadAllText(viewFilename);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    JObject optionsJson = JObject.Parse(fileContent);
                    json["view"] = optionsJson;
                }
            }
            */
        }


        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage Settings(string Template)
        {
            string Data = (string)ActiveModule.ModuleSettings["data"];
            JObject json = new JObject();
            try
            {
                string TemplateFilename = HostingEnvironment.MapPath("~/" + Template);
                string prefix = Path.GetFileNameWithoutExtension(TemplateFilename) + "-";

                string DesktopFolder = HostingEnvironment.MapPath("~/DesktopModules/OpenFiles/");
                string PortalFolder = HostingEnvironment.MapPath(PortalSettings.HomeDirectory + "/OpenFiles/");
                GetJson(json, DesktopFolder, PortalFolder, prefix);

                if (!string.IsNullOrEmpty(Data))
                {
                    try
                    {
                        json["data"] = JObject.Parse(Data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Settings Json Data : " + Data, ex);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
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
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage EditImages(int id)
        {
            //string Template = "DesktopModules/OpenFiles/";
            JObject json = new JObject();
            try
            {
                string desktopFolder = HostingEnvironment.MapPath("~/DesktopModules/OpenFiles/");
                string portalFolder = HostingEnvironment.MapPath(PortalSettings.HomeDirectory + "/OpenFiles/");
                GetJson(json, desktopFolder, portalFolder, "images");

                int moduleId = ActiveModule.ModuleID;
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
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

    }

}

