using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DotNetNuke.Entities.Users;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Datasource.search;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Lucene.Config;
using Satrabel.OpenFiles.Components.Lucene;
using Satrabel.OpenFiles.Components.Utils;

namespace Satrabel.OpenFiles.Components.JPList
{
    class QueryBuilder
    {
        private readonly FieldConfig IndexConfig;
        public Select Select { get; private set; }
        public QueryBuilder(FieldConfig config)
        {
            this.IndexConfig = config;
            Select = new Select();
            //Select.PageSize = 100;
        }
        //public QueryBuilder Build(bool addWorkflowFilter, string cultureCode, IList<UserRoleInfo> roles, NameValueCollection queryString = null)
        //{

        //    BuildFilter(addWorkflowFilter, cultureCode, roles, queryString);

        //    return this;
        //}


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

            if (IndexConfig != null && IndexConfig.Fields != null && IndexConfig.Fields.ContainsKey(AppConfig.FieldNamePublishStatus))
            {
                filter.AddRule(new FilterRule()
                {
                    Field = AppConfig.FieldNamePublishStatus,
                    Value = new StringRuleValue("published"),
                    FieldType = FieldTypeEnum.KEY
                });
            }
            if (IndexConfig != null && IndexConfig.Fields != null && IndexConfig.Fields.ContainsKey(AppConfig.FieldNamePublishStartDate))
            {
                //DateTime startDate = DateTime.MinValue;
                //DateTime endDate = DateTime.Today;
                filter.AddRule(new FilterRule()
                {
                    Field = AppConfig.FieldNamePublishStartDate,
                    Value = new DateTimeRuleValue(DateTime.Today),
                    FieldOperator = OperatorEnum.LESS_THEN_OR_EQUALS,
                    FieldType = FieldTypeEnum.DATETIME
                });
            }
            if (IndexConfig != null && IndexConfig.Fields != null && IndexConfig.Fields.ContainsKey(AppConfig.FieldNamePublishEndDate))
            {
                //DateTime startDate = DateTime.Today;
                //DateTime endDate = DateTime.MaxValue;
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
            if (IndexConfig != null && IndexConfig.Fields != null && IndexConfig.Fields.ContainsKey("userrole"))
            {
                fieldName = "userrole";
            }
            else if (IndexConfig != null && IndexConfig.Fields != null && IndexConfig.Fields.ContainsKey("userroles"))
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

        //internal Select MergeJpListQuery(List<StatusDTO> statuses)
        //{
        //    var select = Select;
        //    var query = select.Query;
        //    foreach (StatusDTO status in statuses)
        //    {
        //        switch (status.action)
        //        {
        //            case "paging":
        //                {
        //                    int number;
        //                    //  string value (it could be number or "all")
        //                    int.TryParse(status.data.number, out number);
        //                    select.PageSize = number;
        //                    select.PageIndex = status.data.currentPage;
        //                    break;
        //                }
        //            case "filter":
        //                {
        //                    if (status.type == "textbox" && status.data != null && !string.IsNullOrEmpty(status.name) && !string.IsNullOrEmpty(status.data.value))
        //                    {
        //                        var names = status.name.Split(',');
        //                        if (names.Length == 1)
        //                        {
        //                            query.AddRule(new FilterRule()
        //                            {
        //                                Field = status.name,
        //                                FieldOperator = OperatorEnum.START_WITH,
        //                                Value = new StringRuleValue(status.data.value),
        //                            });
        //                        }
        //                        else
        //                        {
        //                            var group = new FilterGroup() { Condition = ConditionEnum.OR };
        //                            foreach (var n in names)
        //                            {
        //                                group.AddRule(new FilterRule()
        //                                {
        //                                    Field = n,
        //                                    FieldOperator = OperatorEnum.START_WITH,
        //                                    Value = new StringRuleValue(status.data.value),
        //                                });
        //                            }
        //                            query.FilterGroups.Add(group);
        //                        }
        //                    }
        //                    else if ((status.type == "checkbox-group-filter" || status.type == "button-filter-group")
        //                                && status.data != null && !string.IsNullOrEmpty(status.name))
        //                    {
        //                        if (status.data.filterType == "pathGroup" && status.data.pathGroup != null && status.data.pathGroup.Count > 0)
        //                        {
        //                            query.AddRule(new FilterRule()
        //                            {
        //                                Field = status.name,
        //                                FieldOperator = OperatorEnum.IN,
        //                                MultiValue = status.data.pathGroup.Select(s => new StringRuleValue(s)),
        //                            });
        //                        }
        //                    }
        //                    else if (status.type == "filter-select" && status.data != null && !string.IsNullOrEmpty(status.name))
        //                    {
        //                        if (status.data.filterType == "path" && !string.IsNullOrEmpty(status.data.path))
        //                        {
        //                            query.AddRule(new FilterRule()
        //                            {
        //                                Field = status.name,
        //                                Value = new StringRuleValue(status.data.path),
        //                            });
        //                        }
        //                    }
        //                    break;
        //                }

        //            case "sort":
        //                {
        //                    select.Sort.Clear();
        //                    select.Sort.Add(new SortRule()
        //                    {
        //                        Field =  status.data.path,
        //                        Descending = status.data.order == "desc",
        //                        //FieldType = FieldTypeEnum.
        //                    });
        //                    break;
        //                }
        //        }
        //    }
        //    return select;
        //}
    }
}
