using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using System;

namespace Satrabel.OpenFiles.Components
{
    public class OpenFilesInfo
    {

        public OpenFilesInfo(IFileInfo file, ContentItem dnnContentItem, JObject jsonContent)
        {
            if (file == null) throw new ArgumentNullException("file");
            if (dnnContentItem == null) throw new ArgumentNullException("dnnContentItem");
            if (jsonContent == null) throw new ArgumentNullException("jsonContent");

            File = file;
            DnnContentItem = dnnContentItem;
            JsonAsJToken = jsonContent;
        }

        public OpenFilesInfo(IFileInfo file)
        {
            if (file == null) throw new ArgumentNullException("file");

            File = file;
            DnnContentItem = null;
            JsonAsJToken = new JObject();
            var rawContent = "not specified";
            try
            {
                if (file.ContentItemID > 0)
                {
                    DnnContentItem = Util.GetContentController().GetContentItem(file.ContentItemID);
                    if (DnnContentItem != null && DnnContentItem.Content.IsJson())
                    {
                        rawContent = DnnContentItem.Content;
                        JsonAsJToken = JObject.Parse(rawContent);
                    }
                    else
                    {
                        Log.Logger.Warn($"Expected Content Item not found for file [{file.RelativePath}].");
                    }
                }
                else
                {
                    Log.Logger.Debug($"No ContentItem available for file [{file.RelativePath}].");
                }

            }
            catch (Exception ex)
            {
                var exep = new Exception($"Cannot open content item of {file.RelativePath}. Raw Content: [{rawContent}].", ex);
                Exceptions.LogException(exep);
            }
        }

        public JObject JsonAsJToken { get; private set; }
        public ContentItem DnnContentItem { get; private set; }
        public IFileInfo File { get; private set; }

    }
}