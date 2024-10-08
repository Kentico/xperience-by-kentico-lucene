namespace Kentico.Xperience.Lucene.Core.Indexing;

internal static class StrategyStorage
{
    public static Dictionary<string, Type> Strategies { get; private set; }
    static StrategyStorage() => Strategies = [];

    public static void AddStrategy<TStrategy>(string strategyName) where TStrategy : ILuceneIndexingStrategy => Strategies.Add(strategyName, typeof(TStrategy));
    public static Type GetOrDefault(string strategyName) =>
        Strategies.TryGetValue(strategyName, out var type)
            ? type
            : typeof(DefaultLuceneIndexingStrategy);
}
