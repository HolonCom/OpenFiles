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
using Satrabel.OpenContent.Components.Json;
using DotNetNuke.Entities.Content.Common;
using Satrabel.OpenContent.Components;

#endregion

namespace Satrabel.OpenFiles.Components
{
    // [SupportedModules("OpenFiles")]
    public class OpenFilesAPIController : DnnApiController
    {
        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage Edit(int id)
        {
            try
            {
                JObject json = GetJson(Config.JsonSchemaFolder, Config.PortalFolder(PortalSettings), "");
                //int moduleId = ActiveModule.ModuleID;
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
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        private JObject GetJson(string desktopFolder, string portalFolder, string prefix)
        {
            JObject json = new JObject();

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
            return json;
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

                JObject json = GetJson(Config.JsonSchemaFolder, Config.PortalFolder(PortalSettings), prefix);

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
                JObject json = GetJson(Config.JsonSchemaFolder, Config.PortalFolder(PortalSettings), "images");

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

