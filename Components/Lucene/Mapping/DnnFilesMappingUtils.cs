using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Version = Lucene.Net.Util.Version;

namespace Satrabel.OpenFiles.Components.Lucene.Mapping
{
    public static class DnnFilesMappingUtils
    {
        internal static PerFieldAnalyzerWrapper GetAnalyser()
        {
            var analyzerList = new List<KeyValuePair<string, Analyzer>>
            {
                new KeyValuePair<string, Analyzer>("PortalId", new KeywordAnalyzer()),
                new KeyValuePair<string, Analyzer>("FileId", new KeywordAnalyzer()),
                new KeyValuePair<string, Analyzer>("Title", new SimpleAnalyzer()),
                new KeyValuePair<string, Analyzer>("FileName", new SimpleAnalyzer()),
                new KeyValuePair<string, Analyzer>("Description", new StandardAnalyzer(Version.LUCENE_30)),
                new KeyValuePair<string, Analyzer>("FileContent", new StandardAnalyzer(Version.LUCENE_30)),
                new KeyValuePair<string, Analyzer>("Folder", new LowercaseKeywordAnalyzer()),
                new KeyValuePair<string, Analyzer>("Category", new KeywordAnalyzer())
            };
            return new PerFieldAnalyzerWrapper(new KeywordAnalyzer(), analyzerList);
        }

        private class LowercaseKeywordAnalyzer : Analyzer
        {

            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                TokenStream tokenStream = new KeywordTokenizer(reader);
                tokenStream = new LowerCaseFilter(tokenStream);
                return tokenStream;
            }
        }

        internal static string[] GetSearchAllFieldList()
        {
            return new[] { "PortalId", "FileId", "FileName", "Title", "Description", "FileContent", "Category" };
        }

        internal static LuceneIndexItem MapLuceneDocumentToData(Document doc)
        {
            return new LuceneIndexItem
            {
                PortalId = Convert.ToInt32(doc.Get("PortalId")),
                FileId = Convert.ToInt32(doc.Get("FileId")),
                Title = doc.Get("Title"),
                FileName = doc.Get("FileName"),
                Description = doc.Get("Description"),
                FileContent = doc.Get("FileContent")
            };
        }

        internal static string GetIndexField()
        {
            return "FileId";
        }
    }
}