using System;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;

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
        public string Id { get; private set; }
        public string Type { get; private set; }
        public string Tenant { get; private set; }
        public DateTime CreatedOnDate { get; private set; }


        public int PortalId => int.Parse(Tenant);
        public int FileId => int.Parse(Id);
        public string FileName { get; set; }
        public string Folder { get; set; }
        public string DisplayName
        {
            get
            {
                string retval = "";
                if (Meta.IsNotEmpty() && Meta["title"].IsNotEmpty())
                    retval = Meta["title"].ToString();

                return retval == "" ? FileName : retval;
            }
        }
        //public string Title { get; set; }
        //public string Description { get; set; }
        //public List<string> Categories { get; private set; }
        public string FileContent { get; set; }
        public JToken Meta { get; set; }
    }
}