using System.Collections.Generic;
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
public sealed class CountryStateRepository : CachedRepositoryBase
{
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
        : base(websiteChannelContext, cache, cacheDependencyBuilderFactory)
    {
        this.countryInfoProvider = countryInfoProvider;
        this.stateInfoProvider = stateInfoProvider;
    }


    /// <summary>
    /// Returns a cached list of all <see cref="CountryInfo"/>.
    /// </summary>
    public async Task<IEnumerable<CountryInfo>> GetCountries(CancellationToken cancellationToken)
    {
        return await GetCached(GetCountriesInternal, [nameof(CountryStateRepository), nameof(GetCountries)], cancellationToken);
    }


    /// <summary>
    /// Returns a cached list of all <see cref="StateInfo"/> for the given country.
    /// </summary>
    public async Task<IEnumerable<StateInfo>> GetStates(int countryId, CancellationToken cancellationToken)
    {
        return await GetCached(
            cancellationToken => GetStatesInternal(countryId, cancellationToken),
            [nameof(CountryStateRepository), nameof(GetStates), countryId],
            cancellationToken);
    }


    private async Task<IEnumerable<CountryInfo>> GetCountriesInternal(CancellationToken cancellationToken)
    {
        return await countryInfoProvider.Get().GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }


    private async Task<IEnumerable<StateInfo>> GetStatesInternal(int countryId, CancellationToken cancellationToken)
    {
        return await stateInfoProvider.Get()
            .WhereEquals(nameof(StateInfo.CountryID), countryId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }
}
