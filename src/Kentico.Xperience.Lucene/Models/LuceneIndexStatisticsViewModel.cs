namespace Kentico.Xperience.Lucene.Models
{
    public class LuceneIndexStatisticsViewModel
    {
        //
        // Summary:
        //     Index name.
        public string? Name { get; set; }

        //
        // Summary:
        //     Date of last update.
        public DateTime UpdatedAt { get; set; }

        //
        // Summary:
        //     Number of records contained in the index
        public int Entries { get; set; }

    }
}
