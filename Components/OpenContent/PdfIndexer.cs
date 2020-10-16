using DotNetNuke.Services.FileSystem;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.FileIndexer;
using Satrabel.OpenFiles.Components.Lucene;

namespace Satrabel.OpenFiles.Components.OpenContent
{
    public class OpenContentPdfIndexer : IFileIndexer
    {
        public bool CanIndex(string file)
        {
            if (int.TryParse(file, out var fileId))
            {
                var f = FileManager.Instance.GetFile(fileId);
                if (f == null) return false;
                return f.Extension == "pdf";
            }
            else
            {
                var f = FileUri.FromPath(file);
                if (f == null) return false;
                return f.Extension == ".pdf";
            }
        }

        public string GetContent(string file)
        {
            if (int.TryParse(file, out var fileId))
            {
                var f = FileManager.Instance.GetFile(fileId);

                var fileContent = FileManager.Instance.GetFileContent(f);
                if (fileContent != null)
                {
                    return PdfParser.ReadPdfFile(fileContent);
                }
                return "";
            }
            else
            {
                var f = FileUri.FromPath(file);
                if (f.FileExists)
                {
                    return PdfParser.ReadPdfFile(f.PhysicalFilePath);
                }
                return "";
            }
        }
    }
}
