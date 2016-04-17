using System.Collections.Generic;

namespace Satrabel.OpenFiles.Components.JPList
{
    class ResultDataDTO<TResultDTO>
    {
        public IEnumerable<TResultDTO> items { get; set; }

        public IEnumerable<ResultBreadcrumbDTO> breadcrumbs { get; set; }
        
    }
}
