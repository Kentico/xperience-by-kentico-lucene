using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <inheritdoc cref="IProductTypeParametersExtractor"/>
    public class GrinderParametersExtractor : IProductTypeParametersExtractor
    {
        /// <inheritdoc/>
        public Task ExtractParameter<T>(IDictionary<string, string> parameters, T product, string _, CancellationToken cancellationToken)
        {
            if (product is ProductGrinder grinder)
            {
                parameters.Add("Type", grinder.GrinderType);

                if (grinder.GrinderType != "Manual")
                {
                    parameters.Add("Power", $"{grinder.GrinderPower:0} Watt");
                }
            }

            return Task.CompletedTask;
        }
    }
}
