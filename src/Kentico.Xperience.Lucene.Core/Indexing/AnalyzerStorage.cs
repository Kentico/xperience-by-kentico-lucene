using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public static class AnalyzerStorage
{
    public static Dictionary<string, Type> Analyzers { get; private set; }
    static AnalyzerStorage() => Analyzers = [];

    public static void AddAnalyzer<TAnalyzer>(string analyzerName) where TAnalyzer : Analyzer
        => Analyzers.Add(analyzerName, typeof(TAnalyzer));
    public static Type GetOrDefault(string analyzerName) =>
        Analyzers.TryGetValue(analyzerName, out var type)
            ? type
            : typeof(StandardAnalyzer);
}
