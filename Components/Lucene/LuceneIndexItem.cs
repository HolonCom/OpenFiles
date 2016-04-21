using System;
using System.Collections.Generic;

namespace Satrabel.OpenFiles.Components.Lucene
{
    public class LuceneIndexItem
    {
        public LuceneIndexItem()
        {
            Type = "dnnfile";
            Categories = new List<string>();
        }
        public string Type { get; private set; }
        public int PortalId { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; }
        public string Folder { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileContent { get; set; }
        public List<string> Categories { get; private set; }
        public DateTime? PublicationDate { get; set; }
    }
}