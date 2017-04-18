using System;
using System.Collections.Generic;
using System.Linq;
using DotNetNuke.Entities.Users;
using Satrabel.OpenContent.Components.Datasource.Search;
using Satrabel.OpenContent.Components.Indexing;
using Satrabel.OpenFiles.Components.Utils;

namespace Satrabel.OpenFiles.Components.JPList
{
    class QueryBuilder
    {
        private readonly FieldConfig _indexConfig;
        public Select Select { get; private set; }
        public QueryBuilder(FieldConfig config)
        {
            this._indexConfig = config;
            Select = new Select();
        }

        internal void BuildFilter(int portalId, string folder, bool addWorkflowFilter, IList<UserRoleInfo> roles)
        {
            var filter = Select.Filter;
            filter.AddRule(new FilterRule()
            {
                Field = LuceneMappingUtils.PortalIdField,
                FieldType = FieldTypeEnum.INTEGER,
                FieldOperator = OperatorEnum.EQUAL,
                Value = new IntegerRuleValue(portalId)
            });
            if (!string.IsNullOrEmpty(folder))
            {
                string wildCardSearchValue = NormalizePath(folder);
                filter.AddRule(new FilterRule()
                {
                    Field = LuceneMappingUtils.FolderField,
                    FieldType = FieldTypeEnum.KEY,
                    FieldOperator = OperatorEnum.START_WITH,
                    Value = new StringRuleValue(wildCardSearchValue)
                });
            }
            if (addWorkflowFilter)
            {
                AddWorkflowFilter(filter);
                AddRolesFilter(filter, roles);
            }
        }

        private void AddWorkflowFilter(FilterGroup filter)
        {

            if (_indexConfig?.Fields != null && _indexConfig.Fields.ContainsKey(AppConfig.FieldNamePublishStatus))
            {
                filter.AddRule(new FilterRule()
                {
                    Field = AppConfig.FieldNamePublishStatus,
                    Value = new StringRuleValue("published"),
                    FieldType = FieldTypeEnum.KEY
                });
            }
            if (_indexConfig?.Fields != null && _indexConfig.Fields.ContainsKey(AppConfig.FieldNamePublishStartDate))
            {
                filter.AddRule(new FilterRule()
                {
                    Field = AppConfig.FieldNamePublishStartDate,
                    Value = new DateTimeRuleValue(DateTime.Today),
                    FieldOperator = OperatorEnum.LESS_THEN_OR_EQUALS,
                    FieldType = FieldTypeEnum.DATETIME
                });
            }
            if (_indexConfig?.Fields != null && _indexConfig.Fields.ContainsKey(AppConfig.FieldNamePublishEndDate))
            {
                filter.AddRule(new FilterRule()
                {
                    Field = AppConfig.FieldNamePublishEndDate,
                    Value = new DateTimeRuleValue(DateTime.Today),
                    FieldOperator = OperatorEnum.GREATER_THEN_OR_EQUALS,
                    FieldType = FieldTypeEnum.DATETIME
                });
            }
        }

        private void AddRolesFilter(FilterGroup filter, IList<UserRoleInfo> roles)
        {
            string fieldName = "";
            if (_indexConfig?.Fields != null && _indexConfig.Fields.ContainsKey("userrole"))
            {
                fieldName = "userrole";
            }
            else if (_indexConfig?.Fields != null && _indexConfig.Fields.ContainsKey("userroles"))
            {
                fieldName = "userroles";
            }
            if (!string.IsNullOrEmpty(fieldName))
            {
                List<string> roleLst;
                if (roles.Any())
                {
                    roleLst = roles.Select(r => r.RoleID.ToString()).ToList();
                }
                else
                {
                    roleLst = new List<string>();
                    roleLst.Add("Unauthenticated");
                }
                roleLst.Add("AllUsers");
                filter.AddRule(new FilterRule()
                {
                    Field = fieldName,
                    FieldOperator = OperatorEnum.IN,
                    MultiValue = roleLst.OrderBy(r => r).Select(r => new StringRuleValue(r)),
                    FieldType = FieldTypeEnum.KEY
                });
            }
        }

        private string NormalizePath(string filePath)
        {
            filePath = filePath.Replace("\\", "/");
            filePath = filePath.Trim('~');
            filePath = filePath.Trim('/');
            return filePath;
        }
    }
}
