using System.Collections.Generic;

namespace Satrabel.OpenFiles.Components.Lucene
{
    public class SearchResults
    {
        public SearchResults()
        {
            ids = new List<LuceneIndexItem>();
        }
        public SearchResults(List<LuceneIndexItem> results)
        {
            ids = results;
        }
        public SearchResults(List<LuceneIndexItem> results, int totalResults)
        {
            ids = results;
            TotalResults = totalResults;
        }
        public int TotalResults { get; internal set; }
        public List<LuceneIndexItem> ids { get; private set; }
    }
}