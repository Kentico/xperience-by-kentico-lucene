using Kentico.Xperience.Lucene.Services;

namespace Kentico.Xperience.Lucene;

public static class StrategyStorage
{
    public static Dictionary<string, Type> Strategies { get; private set; }
    static StrategyStorage()
    {
        Strategies = new Dictionary<string, Type>();
    }

    public static void AddStrategy<TStrategy>(string strategyName) where TStrategy : ILuceneIndexingStrategy, new()
    {
        Strategies.Add(strategyName, typeof(TStrategy));
    }
}
