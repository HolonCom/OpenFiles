using System;
using DotNetNuke.Services.FileSystem;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Indexing;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Lucene.Mapping;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Lucene;

namespace Satrabel.OpenFiles.Components.Utils
{
    public static class LuceneMappingUtils
    {
        #region Consts

        private const string ITEM_TYPE_VALUE = "dnnfile";

        /// <summary>
        /// The name of the field which holds the type.
        /// </summary>
        private const string ITEM_TYPE_FIELD = "$type";

        private const string TENANT_FIELD = "$tenant";


        /// <summary>
        /// The name of the field which holds the JSON-serialized source of the object.
        /// </summary>
        private const string FIELD_SOURCE = "$source";

        /// <summary>
        /// The name of the field which holds the timestamp when the document was created.
        /// </summary>
        private const string FIELD_TIMESTAMP = "$timestamp";

        private const string ITEM_ID_FIELD = "$id";
        private const string CREATED_ON_DATE_FIELD = "$createdondate";
        private const string FILE_ID_FIELD = "FileId";
        private const string FILENAME_FIELD = "FileName";
        private const string DISPLAYNAME_FIELD = "DisplayName";
        private const string FILE_CONTENT_FIELD = "FileContent";

        public const string PORTAL_ID_FIELD = "PortalId";
        public const string FOLDER_FIELD = "Folder";
        public const string META_FIELD = "meta";

        #endregion

        internal static Document CreateLuceneDocument(LuceneIndexItem item, FieldConfig config, bool storeSource = false)
        {
            Document luceneDoc = new Document();
            luceneDoc.Add(new Field(ITEM_TYPE_FIELD, item.Type, Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(TENANT_FIELD, item.Tenant, Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(ITEM_ID_FIELD, item.Id, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (storeSource)
            {
                luceneDoc.Add(new Field(FIELD_SOURCE, item.ToJson(), Field.Store.YES, Field.Index.NO));
            }
            luceneDoc.Add(new NumericField(FIELD_TIMESTAMP, Field.Store.YES, true).SetLongValue(DateTime.UtcNow.Ticks));
            luceneDoc.Add(new NumericField(CREATED_ON_DATE_FIELD, Field.Store.NO, true).SetLongValue(item.CreatedOnDate.Ticks));

            luceneDoc.Add(new Field(PORTAL_ID_FIELD, item.PortalId.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field(FILE_ID_FIELD, item.FileId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(FILENAME_FIELD, item.FileName, Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field(FOLDER_FIELD, QueryParser.Escape(item.Folder) , Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(DISPLAYNAME_FIELD, item.DisplayName, Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field(FILE_CONTENT_FIELD, string.IsNullOrEmpty(item.FileContent) ? "" : item.FileContent, Field.Store.YES, Field.Index.ANALYZED));
            var objectMapper = new JsonObjectMapper();
            objectMapper.AddJsonToDocument(item.Meta, luceneDoc, config);

            return luceneDoc;
        }

        internal static LuceneIndexItem CreateLuceneItem(Document doc)
        {
            return new LuceneIndexItem(ITEM_TYPE_VALUE, doc.Get(TENANT_FIELD), doc.Get(CREATED_ON_DATE_FIELD).TicksToDateTime(), doc.Get(ITEM_ID_FIELD))
            {
                FileName = doc.Get(FILENAME_FIELD),
                Folder = doc.Get(FOLDER_FIELD),
                FileContent = doc.Get(FILE_CONTENT_FIELD),
                Meta = doc.Get(META_FIELD),
            };
        }

        internal static LuceneIndexItem CreateLuceneItem(int portalid, int fileid)
        {
            var indexData = new LuceneIndexItem(ITEM_TYPE_VALUE, portalid.ToString(), DateTime.Now, fileid.ToString());
            return indexData;
        }
        public static LuceneIndexItem CreateLuceneItem(IFileInfo file, FieldConfig indexConfig)
        {
            var filesInfo = new OpenFilesInfo(file);
            return CreateLuceneItem(filesInfo, indexConfig);
        }

        internal static LuceneIndexItem CreateLuceneItem(OpenFilesInfo fileInfo, FieldConfig indexConfig)
        {
            var luceneItem = new LuceneIndexItem(ITEM_TYPE_VALUE, fileInfo.File.PortalId.ToString(), fileInfo.File.CreatedOnDate, fileInfo.File.FileId.ToString())
                {
                    FileName = fileInfo.File.FileName,
                    Folder = fileInfo.File.Folder.TrimEnd('/'),
                    FileContent = DnnFilesRepository.GetFileContent(fileInfo.File)
                };

            JObject custom = fileInfo.JsonAsJToken;
            custom.MakeSureFieldExists(META_FIELD, JTokenType.Object);
            custom[META_FIELD].HydrateDefaultFields(indexConfig);

            if (custom[META_FIELD].HasValues)
            {
                luceneItem.Meta = custom[META_FIELD];
            }

            return luceneItem;
        }

        private static Filter GetTypeTenantFilter(string type, string tenant)
        {
            var query = new BooleanQuery();

            var typeTermQuery = new TermQuery(new Term(ITEM_TYPE_FIELD, type));
            query.Add(typeTermQuery, Occur.MUST);

            var tenantTermQuery = new TermQuery(new Term(TENANT_FIELD, tenant));
            query.Add(tenantTermQuery, Occur.MUST);

            Filter filter = new QueryWrapperFilter(query);
            return filter;
        }

        public static Analyzer GetAnalyser()
        {
            var analyser = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30);
            return analyser;
        }

        public static Query GetDeleteQuery(LuceneIndexItem data)
        {
            var selection = new TermQuery(new Term(LuceneMappingUtils.GetIndexFieldName(), LuceneMappingUtils.GetIndexFieldValue(data)));
            return new FilteredQuery(selection, LuceneMappingUtils.GetTypeTenantFilter(ITEM_TYPE_VALUE, data.PortalId.ToString()));
        }

        public static Query GetDeleteFolderQuery(int portalId, string folderPath)
        {
            var selection = new TermQuery(new Term(LuceneMappingUtils.FOLDER_FIELD, folderPath.TrimEnd('/')));
            return new FilteredQuery(selection, LuceneMappingUtils.GetTypeTenantFilter(ITEM_TYPE_VALUE, portalId.ToString()));
        }

        #region private methods

        private static string GetIndexFieldName()
        {
            return FILE_ID_FIELD;
        }

        private static string GetIndexFieldValue(LuceneIndexItem data)
        {
            return data.FileId.ToString();
        }

        #endregion


    }
}