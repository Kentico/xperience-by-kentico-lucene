using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Infrastructure;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of links to social networks.
    /// </summary>
    public class SocialLinkRepository
    {
        private readonly IPageRetriever pageRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        /// <summary>
        /// Initializes a new instance of the <see cref="SocialLinkRepository"/> class that returns links.
        /// </summary>
        /// <param name="pageRetriever">Retriever for pages based on given parameters.</param>
        public SocialLinkRepository(IPageRetriever pageRetriever, RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageRetriever = pageRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
        }


        /// <summary>
        /// Returns an enumerable collection of links to social networks ordered by a position in the content tree.
        /// </summary>
        public async Task<IEnumerable<SocialLink>> GetSocialLinks(CancellationToken? cancellationToken = null)
        {
            const int maxLevel = 2;

            var socialLinks = pageRetriever.Retrieve<SocialLink>(
                query => query
                    .OrderByAscending("NodeOrder"),
                cache => cache
                    .Key($"{nameof(SocialLinkRepository)}|{nameof(GetSocialLinks)}")
                    // Include path dependency to flush cache when a new child page is created or page order is changed.
                    .Dependencies((_, builder) => builder.Pages(SocialLink.CLASS_NAME).PageOrder()));

            return await repositoryCacheHelper.CacheData(async cancellationToken =>
            {
                var socialLinksWithLinkedItems = new List<SocialLink>();

                foreach (var socialLink in socialLinks)
                {
                    socialLinksWithLinkedItems.Add(await socialLink.WithLinkedItems(maxLevel, cancellationToken));
                }

                return socialLinksWithLinkedItems;
            }, cancellationToken ?? CancellationToken.None,
               $"{nameof(SocialLinkRepository)}|{nameof(GetSocialLinks)}|WithLinkedItems",
               () => socialLinks.GetLinkedItemsCacheDependencyKeys(maxLevel, cancellationToken));
        }
    }
}