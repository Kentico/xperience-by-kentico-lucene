namespace Kentico.Xperience.Lucene.Models
{
    public class LuceneSearchResultModel<T>
    {
        public IEnumerable<T>? Hits { get; set; }
        public int TotalHits { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }
    }
}
