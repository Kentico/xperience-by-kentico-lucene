namespace Kentico.Xperience.Lucene.Indexing;

public record IndexStorageModel(string Path, string TaxonomyPath, int Generation, bool IsPublished);
