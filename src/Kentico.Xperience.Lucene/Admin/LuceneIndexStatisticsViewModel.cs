namespace Kentico.Xperience.Lucene.Admin;

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
