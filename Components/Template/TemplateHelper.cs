using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using System;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Satrabel.OpenFiles.Components.Template
{
    public static class TemplateHelper
    {
        public static dynamic GetDocumentModel(string folder)
        {
            PortalSettings ps = PortalSettings.Current;
            string physicalTemplateFolder = HostingEnvironment.MapPath("~/DesktopModules/OpenFiles/");
            dynamic model = new ExpandoObject();
            // schema
            string schemaFilename = physicalTemplateFolder + "schema.json";
            try
            {
                dynamic schema = JsonUtils.JsonToDynamic(File.ReadAllText(schemaFilename));
                model.Schema = schema;
            }
            catch (Exception ex)
            {
                //Exceptions.ProcessModuleLoadException(string.Format("Invalid json-schema. Please verify file {0}.", schemaFilename), this, ex, true);
            }
            // options
            JToken optionsJson = null;
            // default options
            string optionsFilename = physicalTemplateFolder + "options.json";
            if (File.Exists(optionsFilename))
            {
                string fileContent = File.ReadAllText(optionsFilename);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    optionsJson = JObject.Parse(fileContent);
                }
            }
            // language options
            optionsFilename = physicalTemplateFolder + "options." + PortalSettings.Current.CultureCode + ".json";
            if (File.Exists(optionsFilename))
            {
                string fileContent = File.ReadAllText(optionsFilename);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    if (optionsJson == null)
                        optionsJson = JObject.Parse(fileContent);
                    else
                        optionsJson = optionsJson.JsonMerge(JObject.Parse(fileContent));
                }
            }
            if (optionsJson != null)
            {
                dynamic Options = JsonUtils.JsonToDynamic(optionsJson.ToString());
                model.Options = Options;
            }
            if (!string.IsNullOrEmpty(folder))
            {
                string homedir = HostingEnvironment.MapPath(ps.HomeDirectory);
                string basedir = HostingEnvironment.MapPath(ps.HomeDirectory + folder);
                if (Directory.Exists(basedir))
                {
                    var dirs = Directory.GetDirectories(basedir, "*", SearchOption.AllDirectories);

                    model.Folders = dirs.Select(d => new FolderInfo()
                    {
                        value = d.Substring(homedir.Length).Replace("\\", "/"),
                        text = d.Substring(basedir.Length + 1).Replace("\\", "/")
                    }).ToList();
                }
                else
                {
                    throw new Exception(string.Format("Folder {0} does not exist", folder));
                }
            }
            return model;
        }

        private static int GetFileIdFromUrl(string url)
        {
            int returnValue = -1;
            //add http
            if (!(url.ToLower().StartsWith("http")))
            {
                if (url.ToLower().StartsWith("/"))
                {
                    url = "http:/" + url;
                }
                else
                {
                    url = "http://" + url;
                }
            }

            Uri u = new Uri(url);

            if (u != null && u.Query != null)
            {
                NameValueCollection @params = HttpUtility.ParseQueryString(u.Query);

                if (@params != null && @params.Count > 0)
                {
                    string fileTicket = @params.Get("fileticket");

                    if (!(string.IsNullOrEmpty(fileTicket)))
                    {
                        try
                        {
                            returnValue = FileLinkClickController.Instance.GetFileIdFromLinkClick(@params);
                        }
                        catch (Exception ex)
                        {
                            returnValue = -1;

                        }
                    }
                }
            }

            return returnValue;
        }
    }

    public class FolderInfo
    {
        public string value { get; set; }
        public string text { get; set; }
    }
}