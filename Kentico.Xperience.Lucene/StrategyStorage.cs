using Kentico.Xperience.Lucene.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene
{
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
}
