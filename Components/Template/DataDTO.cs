using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Satrabel.OpenFiles.Components.Template
{
    public class DataDTO
    {
        public List<FileDTO> Files { get; set; }
        public dynamic Schema { get; set; }
        public dynamic Options { get; set; }
        public dynamic Settings { get; set; }
        public dynamic Context { get; set; }
    }
}