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
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenContent.Components.Lucene.Mapping;
using Satrabel.OpenFiles.Components.ExternalData;
using Satrabel.OpenFiles.Components.Lucene;

namespace Satrabel.OpenFiles.Components.Utils
{
    public static class LuceneMappingUtils
    {
        #region Consts

        public static readonly string ItemTypeValue = "dnnfile";

        /// <summary>
        /// The name of the field which holds the type.
        /// </summary>
        public static readonly string ItemTypeField = "$type";
        public static readonly string TenantField = "$tenant";


        /// <summary>
        /// The name of the field which holds the JSON-serialized source of the object.
        /// </summary>
        public static readonly string FieldSource = "$source";

        /// <summary>
        /// The name of the field which holds the timestamp when the document was created.
        /// </summary>
        public static readonly string FieldTimestamp = "$timestamp";
        public static readonly string ItemIdField = "$id";
        public static readonly string FieldCreatedOnDate = "$createdondate";

        public static readonly string PortalIdField = "PortalId";
        public static readonly string FileIdField = "FileId";
        public static readonly string FileNameField = "FileName";
        public static readonly string FolderField = "Folder";
        public static readonly string FileContentField = "FileContent";
        public static readonly string MetaField = "meta";

        #endregion

        internal static Document CreateLuceneDocument(LuceneIndexItem item, FieldConfig config, bool storeSource = false)
        {
            Document luceneDoc = new Document();
            luceneDoc.Add(new Field(ItemTypeField, item.Type, Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(TenantField, item.Tenant, Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(ItemIdField, item.Id, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (storeSource)
            {
                luceneDoc.Add(new Field(FieldSource, item.ToJson(), Field.Store.YES, Field.Index.NO));
            }
            luceneDoc.Add(new NumericField(FieldTimestamp, Field.Store.YES, true).SetLongValue(DateTime.UtcNow.Ticks));
            luceneDoc.Add(new NumericField(FieldCreatedOnDate, Field.Store.NO, true).SetLongValue(item.CreatedOnDate.Ticks));

            luceneDoc.Add(new Field(PortalIdField, item.PortalId.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field(FileIdField, item.FileId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(FileNameField, item.FileName, Field.Store.YES, Field.Index.ANALYZED));
            luceneDoc.Add(new Field(FolderField, QueryParser.Escape(item.Folder) , Field.Store.YES, Field.Index.NOT_ANALYZED));
            luceneDoc.Add(new Field(FileContentField, string.IsNullOrEmpty(item.FileContent) ? "" : item.FileContent, Field.Store.YES, Field.Index.ANALYZED));
            var objectMapper = new JsonObjectMapper();
            objectMapper.AddJsonToDocument(item.Meta, luceneDoc, config);

            return luceneDoc;
        }

        internal static LuceneIndexItem CreateLuceneItem(Document doc)
        {
            return new LuceneIndexItem(ItemTypeValue, doc.Get(TenantField), doc.Get(FieldCreatedOnDate).TicksToDateTime(), doc.Get(ItemIdField))
            {
                FileName = doc.Get(FileNameField),
                Folder = doc.Get(FolderField),
                FileContent = doc.Get(FileContentField),
                Meta = doc.Get(MetaField),
            };
        }

        internal static LuceneIndexItem CreateLuceneItem(int portalid, int fileid)
        {
            var indexData = new LuceneIndexItem(ItemTypeValue, portalid.ToString(), DateTime.Now, fileid.ToString());
            return indexData;
        }
        public static LuceneIndexItem CreateLuceneItem(IFileInfo file, FieldConfig indexConfig)
        {
            var filesInfo = new OpenFilesInfo(file);
            return CreateLuceneItem(filesInfo, indexConfig);
        }

        internal static LuceneIndexItem CreateLuceneItem(OpenFilesInfo fileInfo, FieldConfig indexConfig)
        {
            var luceneItem = new LuceneIndexItem(ItemTypeValue, fileInfo.File.PortalId.ToString(), fileInfo.File.CreatedOnDate, fileInfo.File.FileId.ToString())
                {
                    FileName = fileInfo.File.FileName,
                    Folder = fileInfo.File.Folder.TrimEnd('/'),
                    FileContent = DnnFilesRepository.GetFileContent(fileInfo.File)
                };

            JObject custom = fileInfo.JsonAsJToken;
            custom.MakeSureFieldExists(MetaField, JTokenType.Object);
            custom[MetaField].HydrateDefaultFields(indexConfig);

            if (custom[MetaField].HasValues)
            {
                luceneItem.Meta = custom[MetaField];
            }

            return luceneItem;
        }

        private static Filter GetTypeTenantFilter(string type, string tenant)
        {
            var query = new BooleanQuery();

            var typeTermQuery = new TermQuery(new Term(ItemTypeField, type));
            query.Add(typeTermQuery, Occur.MUST);

            var tenantTermQuery = new TermQuery(new Term(TenantField, tenant));
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
            return new FilteredQuery(selection, LuceneMappingUtils.GetTypeTenantFilter(ItemTypeValue, data.PortalId.ToString()));
        }

        public static Query GetDeleteFolderQuery(int portalId, string folderPath)
        {
            var selection = new TermQuery(new Term(LuceneMappingUtils.FolderField, folderPath.TrimEnd('/')));
            return new FilteredQuery(selection, LuceneMappingUtils.GetTypeTenantFilter(ItemTypeValue, portalId.ToString()));
        }

        #region private methods

        private static string GetIndexFieldName()
        {
            return FileIdField;
        }

        private static string GetIndexFieldValue(LuceneIndexItem data)
        {
            return data.FileId.ToString();
        }

        #endregion


    }
}