using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Satrabel.OpenDocument.Components.JPList
{
    public class FilterDTO
    {
        public string name { get; set; }

        public string value { get; set; }

        public string path { get; set; }

        public List<string> pathGroup { get; set; }
    }
}
