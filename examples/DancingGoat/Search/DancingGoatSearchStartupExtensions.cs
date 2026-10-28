namespace DancingGoat.Search;

public static class DancingGoatSearchStartupExtensions
{
    public static IServiceCollection AddDancingGoatLuceneServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKenticoLucene(builder =>
        {
            builder.RegisterStrategy<DancingGoatSearchIndexingStrategy>("DancingGoatGlobalSearchWithTaxonomyFacetsV2");
        }, configuration);

        services.AddSingleton<DancingGoatSearchService>();

        return services;
    }
}
