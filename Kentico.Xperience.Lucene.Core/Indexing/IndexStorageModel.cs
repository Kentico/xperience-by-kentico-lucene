namespace Kentico.Xperience.Lucene.Core.Indexing;

public record IndexStorageModel(string Path, string TaxonomyPath, int Generation, bool IsPublished);
