using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Satrabel.OpenFiles.Components.JPList;

namespace Satrabel.OpenFiles.Components.Lucene
{
    public static class LuceneQueryBuilder
    {
        public static string BuildLuceneQuery(JpListQueryDTO jpListQuery)
        {
            string queryStr = "";
            if (jpListQuery.Filters.Any())
            {
                foreach (FilterDTO f in jpListQuery.Filters)
                {
                    if (f.ExactSearchMultiValue != null && f.ExactSearchMultiValue.Any()) //group is bv multicheckbox, vb categories where(categy="" OR category="")
                    {
                        string pathStr = "";
                        foreach (var p in f.ExactSearchMultiValue)
                        {
                            pathStr += (string.IsNullOrEmpty(pathStr) ? "" : " OR ") + f.Name + ":\"" + p + "\"";
                        }

                        queryStr += "+" + "(" + pathStr + ")";
                    }
                    else
                    {
                        var names = f.Names;
                        string pathStr = "";
                        foreach (var n in names)
                        {
                            if (!string.IsNullOrEmpty(f.ExactSearchValue))
                            {
                                pathStr += (string.IsNullOrEmpty(pathStr) ? "" : " OR ") + n + ":\"" + f.ExactSearchValue + "\"";  //for dropdownlists; value is keyword => never partial search
                            }
                            else
                            {
                                pathStr += (string.IsNullOrEmpty(pathStr) ? "" : " OR ") + n + ":\"" + f.WildCardSearchValue + "*\"";   //textbox
                            }
                        }
                        queryStr += "+" + "(" + pathStr + ")";
                    }
                }
            }
            return queryStr;
        }

    }
}