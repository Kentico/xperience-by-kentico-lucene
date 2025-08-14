namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Represents configuration options for automatic reindexing based on differing assembly versions.
/// </summary>
public sealed class AutomaticReindexingOptions
{
    /// <summary>
    /// If true, the assembly versions will be checked and the differing indexes will be rebuilt.
    /// </summary>
    public bool Enabled { get; set; } = false;


    /// <summary>
    /// The delay in minutes between differing assembly versions check.
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 1;


    /// <summary>
    /// Index names that are excluded from automatic assembly version check and reindexing if they differ.
    /// </summary>
    public List<string> IndexesExcludedFromAutomaticReindexing { get; set; } = [];
}
