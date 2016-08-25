using System;
using Newtonsoft.Json.Linq;

namespace Satrabel.OpenFiles.Components.Lucene
{
    public class LuceneIndexItem
    {
        public LuceneIndexItem(string itemType, string tenant, DateTime createdOnDate, string itemId)
        {
            Type = itemType; ;
            Tenant = tenant;
            Id = itemId;
            CreatedOnDate = createdOnDate;
        }
        public string Id { get; set; }
        public string Type { get; private set; }
        public string Tenant { get; set; }
        public DateTime CreatedOnDate { get; set; }


        public int PortalId
        {
            get
            {
                return int.Parse(Tenant);
            }
        }
        public int FileId
        {
            get
            {
                return int.Parse(Id);
            }
        }
        public string FileName { get; set; }
        public string Folder { get; set; }
        //public string Title { get; set; }
        //public string Description { get; set; }
        //public List<string> Categories { get; private set; }
        public string FileContent { get; set; }
        public JToken Meta { get; set; }
    }
}