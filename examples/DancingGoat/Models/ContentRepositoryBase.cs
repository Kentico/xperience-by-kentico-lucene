using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

namespace DancingGoat.Models
{
    /// <summary>
    /// Shared implementation for content type repositories.
    /// </summary>
    public abstract class ContentRepositoryBase
    {
        /// <summary>
        /// Website channel context.
        /// </summary>
        protected IWebsiteChannelContext WebsiteChannelContext { get; init; }


        private readonly IContentQueryExecutor executor;
        private readonly IProgressiveCache cache;


        /// <summary>
        /// Initializes a new instance of the <see cref="ContentRepositoryBase"/> class.
        /// </summary>
        /// <param name="websiteChannelContext">Website channel context.</param>
        /// <param name="executor">Content query executor.</param>
        /// <param name="cache">Cache.</param>
        public ContentRepositoryBase(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
        {
            WebsiteChannelContext = websiteChannelContext;
            this.executor = executor;
            this.cache = cache;
        }


        /// <summary>
        /// Returns cached query result.
        /// </summary>
        /// <typeparam name="T">Model to which the query results will be mapped.</typeparam>
        /// <param name="queryBuilder">Prepared query builder to be executed by the injected <see cref="IContentQueryExecutor"/>.</param>
        /// <param name="queryOptions">Optional <see cref="ContentQueryExecutionOptions"/>. Default values are used if not specified.</param>
        /// <param name="cacheSettings">Object with values to set up cache. See <see cref="CacheSettings"/> for more information.</param>
        /// <param name="cacheDependenciesFunc">Function that will create cache dependencies for the query.</param>
        /// <param name="cancellationToken">Cancellation instruction.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the <paramref name="queryBuilder"/>, <paramref name="cacheSettings"/> or <paramref name="cacheDependenciesFunc"/> parameters is null.</exception>
        /// <remarks>Request is not cached if the request is for preview.</remarks>
        public Task<IEnumerable<T>> GetCachedQueryResult<T>(
            ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions,
            CacheSettings cacheSettings,
            Func<IEnumerable<T>, CancellationToken, Task<ISet<string>>> cacheDependenciesFunc,
            CancellationToken cancellationToken)
        {
            if (queryBuilder is null)
            {
                throw new ArgumentNullException(nameof(queryBuilder));
            }

            if (cacheSettings is null)
            {
                throw new ArgumentNullException(nameof(cacheSettings));
            }

            if (cacheDependenciesFunc is null)
            {
                throw new ArgumentNullException(nameof(cacheDependenciesFunc));
            }

            if (queryOptions == null)
            {
                queryOptions = new ContentQueryExecutionOptions();
            }

            return GetCachedQueryResultInternal(queryBuilder, queryOptions, cacheSettings, cacheDependenciesFunc, cancellationToken);
        }


        private async Task<IEnumerable<T>> GetCachedQueryResultInternal<T>(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions,
            CacheSettings cacheSettings,
            Func<IEnumerable<T>, CancellationToken, Task<ISet<string>>> cacheDependenciesFunc,
            CancellationToken cancellationToken)
        {
            queryOptions.ForPreview = WebsiteChannelContext.IsPreview;
            queryOptions.IncludeSecuredItems = queryOptions.IncludeSecuredItems || WebsiteChannelContext.IsPreview;

            if (WebsiteChannelContext.IsPreview)
            {
                return await executor.GetMappedResult<T>(queryBuilder, queryOptions, cancellationToken);
            }

            return await cache.LoadAsync(async (cacheSettings) =>
            {
                var result = await executor.GetMappedResult<T>(queryBuilder, queryOptions, cancellationToken);

                if (cacheSettings.Cached = result != null && result.Any())
                {
                    cacheSettings.CacheDependency = CacheHelper.GetCacheDependency(await cacheDependenciesFunc(result, cancellationToken));
                }

                return result;
            }, cacheSettings);
        }
    }
}
