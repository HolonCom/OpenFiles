using DotNetNuke.Services.Exceptions;

namespace Satrabel.OpenFiles.Components.JPList
{
    class ResultExtDTO<TResultDTO>
    {
        public ResultDataDTO<TResultDTO> data { get; set; }

        public int count { get; set; }
    }
}
