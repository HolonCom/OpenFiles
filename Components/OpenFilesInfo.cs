using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using DotNetNuke.ComponentModel.DataAnnotations;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;
using Satrabel.OpenFiles.Components.ExternalData;

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
            try
            {
                if (file.ContentItemID > 0)
                {
                    DnnContentItem = Util.GetContentController().GetContentItem(file.ContentItemID);
                    JsonAsJToken = JObject.Parse(DnnContentItem.Content);
                }
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }

        }

        public JObject JsonAsJToken { get; private set; }
        public ContentItem DnnContentItem { get; private set; }
        public IFileInfo File { get; private set; }

    }
}