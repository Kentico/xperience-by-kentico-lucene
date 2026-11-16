namespace DancingGoat.Search;

public static class DancingGoatSearchStartupExtensions
{
    public static IServiceCollection AddDancingGoatLuceneServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKenticoLucene(builder =>
        {
            builder.RegisterStrategy<DancingGoatSearchIndexingStrategy>(DancingGoatSearchIndexingStrategy.STRATEGY_NAME);
        }, configuration);

        services.AddSingleton<DancingGoatSearchService>();

        return services;
    }
}
