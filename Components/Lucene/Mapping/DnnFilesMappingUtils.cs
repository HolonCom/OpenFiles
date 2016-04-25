using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Alpaca;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenContent.Components.Lucene.Mapping;
using Satrabel.OpenFiles.Components.ExternalData;

namespace Satrabel.OpenFiles.Components.Lucene.Mapping
{
    public static class DnnFilesMappingUtils
    {
        #region Consts
        /// <summary>
        /// The name of the field which holds the type.
        /// </summary>
        public static readonly string FieldType = "$type";
        /// <summary>
        /// The name of the field which holds the JSON-serialized source of the object.
        /// </summary>
        public static readonly string FieldSource = "$source";

        /// <summary>
        /// The name of the field which holds the timestamp when the document was created.
        /// </summary>
        public static readonly string FieldTimestamp = "$timestamp";
        public static readonly string FieldId = "$id";
        #endregion

        //internal static string[] GetSearchAllFieldList()
        //{
        //    return new[] { "PortalId", "FileId", "FileName", "Title", "Description", "PublicationDate", "FileContent", "Category" };
        //}

        public static Document DataItemToLuceneDocument(LuceneIndexItem data, bool storeSource = false)
        {
            FieldConfig indexJson = FilesRepository.GetIndexJson();
            return DataItemToLuceneDocument(data.Type, data.FileId.ToString(), data, indexJson, storeSource);
        }

        private static Document DataItemToLuceneDocument(string type, string id, LuceneIndexItem item, FieldConfig config, bool storeSource = false)
        {
            Document luceneDoc = new Document();
            luceneDoc.Add(new Field(FieldType, type, Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(FieldId, id, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (storeSource)
            {
                luceneDoc.Add(new Field(FieldSource, item.ToJson(), Field.Store.YES, Field.Index.NO));
            }
            luceneDoc.Add(new NumericField(FieldTimestamp, Field.Store.YES, true).SetLongValue(DateTime.UtcNow.Ticks));

            luceneDoc.Add(new Field("PortalId", item.PortalId.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field("FileId", item.FileId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field("FileName", item.FileName, Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field("Folder", item.Folder, Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field("FileContent", string.IsNullOrEmpty(item.FileContent) ? "" : item.FileContent, Field.Store.YES, Field.Index.ANALYZED));
            var objectMapper = new JsonObjectMapper();
            objectMapper.AddJsonToDocument(item.Meta, luceneDoc, config);
            //luceneDoc.Add(new Field("Title", string.IsNullOrEmpty(item.Title) ? "" : item.Title, Field.Store.YES, Field.Index.ANALYZED));
            //luceneDoc.Add(new Field("Description", string.IsNullOrEmpty(item.Description) ? "" : item.Description, Field.Store.YES, Field.Index.ANALYZED));
            //luceneDoc.Add(new Field("PublicationDate", string.IsNullOrEmpty(item.PublicationDate) ? "" : item.PublicationDate, Field.Store.YES, Field.Index.ANALYZED));
            //if (item.Categories != null)
            //{
            //    foreach (var cat in item.Categories)
            //    {
            //        luceneDoc.Add(new Field("Category", cat, Field.Store.NO, Field.Index.ANALYZED));
            //    }
            //}

            return luceneDoc;
        }

        internal static LuceneIndexItem MapLuceneDocumentToData(Document doc)
        {
            return new LuceneIndexItem
            {
                PortalId = Convert.ToInt32(doc.Get("PortalId")),
                FileId = Convert.ToInt32(doc.Get("FileId")),
                FileName = doc.Get("FileName"),
                FileContent = doc.Get("FileContent"),
                Meta = doc.Get("Meta"),
                //Title = doc.Get("Title"),
                //Description = doc.Get("Description"),
                //PublicationDate = doc.Get("PublicationDate")
            };
        }

        internal static string GetIndexFieldName()
        {
            return "FileId";
        }
        internal static string GetIndexFieldValue(LuceneIndexItem data)
        {
            return data.FileId.ToString();
        }
        public static Filter GetTypeFilter(string type)
        {
            var typeTermQuery = new TermQuery(new Term(FieldType, type));
            BooleanQuery query = new BooleanQuery();
            query.Add(typeTermQuery, Occur.MUST);
            Filter filter = new QueryWrapperFilter(query);
            return filter;
        }

        public static Analyzer GetAnalyser()
        {
            var analyser = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30);
            return analyser;
        }


        //private class LowercaseKeywordAnalyzer : Analyzer
        //{
        //    public override TokenStream TokenStream(string fieldName, TextReader reader)
        //    {
        //        TokenStream tokenStream = new KeywordTokenizer(reader);
        //        tokenStream = new LowerCaseFilter(tokenStream);
        //        return tokenStream;
        //    }
        //}

        //internal static PerFieldAnalyzerWrapper GetAnalyser()
        //{
        //    var analyzerList = new List<KeyValuePair<string, Analyzer>>
        //    {
        //        new KeyValuePair<string, Analyzer>("PortalId", new KeywordAnalyzer()),
        //        new KeyValuePair<string, Analyzer>("FileId", new KeywordAnalyzer()),
        //        new KeyValuePair<string, Analyzer>("Title", new SimpleAnalyzer()),
        //        new KeyValuePair<string, Analyzer>("FileName", new SimpleAnalyzer()),
        //        new KeyValuePair<string, Analyzer>("Description", new StandardAnalyzer(Version.LUCENE_30)),
        //        new KeyValuePair<string, Analyzer>("FileContent", new StandardAnalyzer(Version.LUCENE_30)),
        //        new KeyValuePair<string, Analyzer>("Folder", new LowercaseKeywordAnalyzer()),
        //        new KeyValuePair<string, Analyzer>("PublicationDate", new LowercaseKeywordAnalyzer()),
        //        new KeyValuePair<string, Analyzer>("Category", new KeywordAnalyzer())
        //    };
        //    return new PerFieldAnalyzerWrapper(new KeywordAnalyzer(), analyzerList);
        //}
    }
}