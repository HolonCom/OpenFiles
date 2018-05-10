using System.IO;
using System.Text;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf;

namespace Satrabel.OpenFiles.Components.Lucene
{
    public static class PdfParser
    {
        public static string ReadPdfFile(string fileName)
        {
            var text = new StringBuilder();

            if (File.Exists(fileName))
            {
                var pdfReader = new PdfReader(fileName);

                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

                    currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                    text.Append(currentText);
                }
                pdfReader.Close();
            }
            return text.ToString();
        }

        public static string ReadPdfFile(Stream fileContent)
        {
            var text = new StringBuilder();

            using (var pdfReader = new PdfReader(fileContent))
            {
                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

                    currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                    text.Append(currentText);
                }
                pdfReader.Close();
            }

            return text.ToString();
        }
    }
}
