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
            TotalResults = ids.Count;
        }
        public SearchResults(List<LuceneIndexItem> results, int totalResults)
        {
            ids = results;
            TotalResults = totalResults;
        }
        public int TotalResults { get; private set; }
        public List<LuceneIndexItem> ids { get; private set; }
    }
}