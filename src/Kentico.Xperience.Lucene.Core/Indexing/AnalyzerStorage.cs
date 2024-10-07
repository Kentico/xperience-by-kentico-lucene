using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal static class AnalyzerStorage
{
    public static Dictionary<string, Type> Analyzers { get; private set; }
    public static LuceneVersion AnalyzerLuceneVersion { get; private set; }
    static AnalyzerStorage() => Analyzers = [];


    public static void SetAnalyzerLuceneVersion(LuceneVersion matchVersion) => AnalyzerLuceneVersion = matchVersion;


    public static void AddAnalyzer<TAnalyzer>(string analyzerName) where TAnalyzer : Analyzer
        => Analyzers.Add(analyzerName, typeof(TAnalyzer));


    public static Type GetOrDefault(string analyzerName) =>
        Analyzers.TryGetValue(analyzerName, out var type)
            ? type
            : typeof(StandardAnalyzer);
}
