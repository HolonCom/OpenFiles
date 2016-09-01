using System;

namespace Satrabel.OpenFiles.Components.Template
{
    public class FileDTO
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string FolderName { get; set; }
        public string Url { get; set; }
        public dynamic Custom { get; set; }

        public string IconUrl { get; set; }

        public int FileId { get; set; }

        public bool IsImage { get; set; }
        public string ImageUrl { get; set; }
        public string EditUrl { get; set; }
        public bool IsEditable { get; set; }
        public DateTime LastModifiedOnDate { get; set; }
        public DateTime CreatedOnDate { get; set; }

        public string FolderPath { get; set; }

        public bool IsFolder { get; set; }
        public int Id { get; set; }
    }
}