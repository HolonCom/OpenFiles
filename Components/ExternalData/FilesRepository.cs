using System;
using System.IO;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Dnn;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Lucene.Config;

namespace Satrabel.OpenFiles.Components.ExternalData
{
    public static class FilesRepository
    {
        internal static FieldConfig GetIndexConfig(int portalId)
        {
            var portal = PortalController.Instance.GetPortal(portalId);
            return GetIndexConfig(portal);
        }

        internal static FieldConfig GetIndexConfig(PortalInfo ps)
        {
            var file = new FileUri(AppConfig.Instance.PortalFolder(ps), "index.json");
            if (!file.FileExists)
            {
                file = new FileUri(AppConfig.Instance.SchemaFolder, "index.json");
            }
            if (file.FileExists)
            {
                string content = File.ReadAllText(file.PhysicalFilePath);
                var indexConfig = JsonConvert.DeserializeObject<FieldConfig>(content);
                return indexConfig;
            }
            throw new Exception("Can not find index.json");
        }

        internal static FieldConfig GetIndexConfig(PortalSettings ps)
        {
            var file = new FileUri(AppConfig.Instance.PortalFolder(ps), "index.json");
            if (!file.FileExists)
            {
                file = new FileUri(AppConfig.Instance.SchemaFolder, "index.json");
            }
            if (file.FileExists)
            {
                string content = File.ReadAllText(file.PhysicalFilePath);
                var indexConfig = JsonConvert.DeserializeObject<FieldConfig>(content);

                //add system field "Folder"
                indexConfig.Fields.Add("Folder", new FieldConfig()
                {
                    Index = true,
                    IndexType = "key",
                    Sort = true
                });
                return indexConfig;
            }
            throw new Exception("Can not find index.json");
        }

        internal static JObject GetSchemaAndOptionsJson(FolderUri desktopFolder, FolderUri portalFolder, string prefix)
        {
            JObject json = new JObject();

            if (!portalFolder.FolderExists)
            {
                Directory.CreateDirectory(portalFolder.PhysicalFullDirectory);
            }
            if (!string.IsNullOrEmpty(prefix))
            {
                prefix = prefix + "-";
            }
            // schema
            string schemaFilename = portalFolder.PhysicalFullDirectory + "\\" + prefix + "schema.json";
            if (!File.Exists(schemaFilename))
            {
                schemaFilename = desktopFolder.PhysicalFullDirectory + "\\" + prefix + "schema.json";
            }
            JObject schemaJson = JObject.Parse(File.ReadAllText(schemaFilename));
            json["schema"] = schemaJson;
            // default options
            string optionsFilename = portalFolder.PhysicalFullDirectory + "\\" + prefix + "options.json";
            if (!File.Exists(optionsFilename))
            {
                optionsFilename = desktopFolder.PhysicalFullDirectory + "\\" + prefix + "options.json";
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
            optionsFilename = portalFolder.PhysicalFullDirectory + "\\" + prefix + "options." + DnnLanguageUtils.GetCurrentCultureCode() + ".json";
            if (!File.Exists(optionsFilename))
            {
                optionsFilename = desktopFolder.PhysicalFullDirectory + "\\" + prefix + "options." + DnnLanguageUtils.GetCurrentCultureCode() + ".json";
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
    }
}