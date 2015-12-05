using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Satrabel.OpenFiles.Components.Template
{
    public class FileDTO
    {
        public string FileName { get; set; }
        public string FolderName { get; set; }
        public string Url { get; set; }
        public dynamic Custom { get; set; }
        public bool IsImage { get; set; }

        public string IconUrl { get; set; }

        public int FileId { get; set; }

        public string ImageUrl { get; set; }
    }
}