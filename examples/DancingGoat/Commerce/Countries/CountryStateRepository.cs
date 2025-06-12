using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.DataEngine;
using CMS.Globalization;
using CMS.Helpers;
using CMS.Websites.Routing;

namespace DancingGoat.Commerce;

/// <summary>
/// Repository for managing country and state information retrieval operations.
/// </summary>
public class CountryStateRepository
{
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly IProgressiveCache cache;
    private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;
    private readonly IInfoProvider<CountryInfo> countryInfoProvider;
    private readonly IInfoProvider<StateInfo> stateInfoProvider;


    /// <summary>
    /// Initializes a new instance of the <see cref="CountryStateRepository"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="cacheDependencyBuilderFactory">The cache dependency builder factory.</param>
    /// <param name="countryInfoProvider">The country info provider.</param>
    /// <param name="stateInfoProvider">The state info provider.</param>
    public CountryStateRepository(IWebsiteChannelContext websiteChannelContext, IProgressiveCache cache, ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
        IInfoProvider<CountryInfo> countryInfoProvider, IInfoProvider<StateInfo> stateInfoProvider)
    {
        this.websiteChannelContext = websiteChannelContext;
        this.cache = cache;
        this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
        this.countryInfoProvider = countryInfoProvider;
        this.stateInfoProvider = stateInfoProvider;
    }


    /// <summary>
    /// Returns a cached list of all <see cref="CountryInfo"/>.
    /// </summary>
    public async Task<IEnumerable<CountryInfo>> GetCountries(CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetCountriesInternal(cancellationToken);
        }

        var cacheSettings = new CacheSettings(5, websiteChannelContext.WebsiteChannelName, nameof(CountryStateRepository), nameof(GetCountries));

        return await cache.LoadAsync(async (cacheSettings) =>
        {
            var result = await GetCountriesInternal(cancellationToken);

            if (cacheSettings.Cached = result != null && result.Any())
            {
                var cacheDependencyBuilder = cacheDependencyBuilderFactory.Create();
                var cacheDependencies = cacheDependencyBuilder
                    .ForInfoObjects<CountryInfo>()
                    .All()
                    .Builder()
                    .Build();
                cacheSettings.CacheDependency = cacheDependencies;
            }
            return result;
        }, cacheSettings);
    }


    private async Task<IEnumerable<CountryInfo>> GetCountriesInternal(CancellationToken cancellationToken)
    {
        return await countryInfoProvider.Get().GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Returns a cached list of all <see cref="StateInfo"/> for the given country.
    /// </summary>
    public async Task<IEnumerable<StateInfo>> GetStates(int countryId, CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetStatesInternal(countryId, cancellationToken);
        }

        var cacheSettings = new CacheSettings(5, websiteChannelContext.WebsiteChannelName, nameof(CountryStateRepository), nameof(GetStates), countryId);

        return await cache.LoadAsync(async (cacheSettings) =>
        {
            var result = await GetStatesInternal(countryId, cancellationToken);

            if (cacheSettings.Cached = result != null && result.Any())
            {
                var cacheDependencyBuilder = cacheDependencyBuilderFactory.Create();
                var cacheDependencies = cacheDependencyBuilder
                    .ForInfoObjects<StateInfo>()
                    .All()
                    .Builder()
                    .Build();
                cacheSettings.CacheDependency = cacheDependencies;
            }

            return result;
        }, cacheSettings);
    }


    private async Task<IEnumerable<StateInfo>> GetStatesInternal(int countryId, CancellationToken cancellationToken)
    {
        return await stateInfoProvider.Get()
            .WhereEquals(nameof(StateInfo.CountryID), countryId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }
}
