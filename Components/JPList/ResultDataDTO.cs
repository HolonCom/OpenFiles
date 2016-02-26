using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Satrabel.OpenFiles.Components.JPList
{
    class ResultDataDTO<TResultDTO>
    {
        public IEnumerable<TResultDTO> items { get; set; }

        public IEnumerable<ResultBreadcrumbDTO> breadcrumbs { get; set; }
        
    }
}
