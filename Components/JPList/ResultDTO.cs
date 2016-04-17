using System.Collections.Generic;

namespace Satrabel.OpenFiles.Components.JPList
{
    class ResultDTO<TResultDTO>
    {
        public IEnumerable<TResultDTO> data { get; set; }

        public int count { get; set; }

        public string query  { get; set; }
        
    }
}
