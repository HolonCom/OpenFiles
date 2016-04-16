using System;
using System.Collections.Generic;

namespace Satrabel.OpenFiles.Components.JPList
{
    public static class JpListQueryBuilder
    {
        internal static JpListQueryDTO MergeJpListQuery(List<StatusDTO> statuses)
        {
            var query = new JpListQueryDTO();
            foreach (StatusDTO status in statuses)
            {
                switch (status.action)
                {
                    case "paging":
                        {
                            int number = 100000;
                            //  string value (it could be number or "all")
                            int.TryParse(status.data.number, out number);
                            query.Pagination = new PaginationDTO()
                            {
                                number = number,
                                currentPage = status.data.currentPage
                            };
                            break;
                        }
                    case "filter":
                        {
                            if (status.type == "textbox" && status.data != null && !String.IsNullOrEmpty(status.name) && !String.IsNullOrEmpty(status.data.value))
                            {
                                query.Filters.Add(new FilterDTO()
                                {
                                    Name = status.name,
                                    WildCardSearchValue = status.data.value,
                                });
                            }
                            else if ((status.type == "checkbox-group-filter" || status.type == "button-filter-group")
                                     && status.data != null && !string.IsNullOrEmpty(status.name))
                            {
                                if (status.data.filterType == "pathGroup" && status.data.pathGroup != null && status.data.pathGroup.Count > 0)
                                {
                                    query.Filters.Add(new FilterDTO()
                                    {
                                        Name = status.name,
                                        ExactSearchMultiValue = status.data.pathGroup
                                    });
                                }
                            }
                            else if (status.type == "filter-select" && status.data != null && !string.IsNullOrEmpty(status.name))
                            {
                                if (status.data.filterType == "path" && status.data.path != null)
                                {
                                    query.Filters.Add(new FilterDTO()
                                    {
                                        Name = status.name,
                                        ExactSearchValue = status.data.path,
                                    });
                                }
                            }
                            break;
                        }

                    case "sort":
                        {
                            query.Sorts.Add(new SortDTO()
                            {
                                path = status.data.path, // field name
                                order = status.data.order
                            });
                            break;
                        }
                }
            }
            return query;
        }
    }
}