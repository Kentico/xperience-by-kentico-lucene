using System;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;

using Microsoft.AspNetCore.Http;

namespace DancingGoat
{
    public class CurrentLanguageRetriever : ICurrentLanguageRetriever
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider;
        private readonly IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider;


        /// <summary>
        /// Initializes an instance of the <see cref="CurrentLanguageRetriever"/> class.
        /// </summary>
        public CurrentLanguageRetriever(
            IHttpContextAccessor httpContextAccessor,
            IWebsiteChannelContext websiteChannelContext,
            IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider,
            IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.websiteChannelContext = websiteChannelContext;
            this.websiteChannelInfoProvider = websiteChannelInfoProvider;
            this.contentLanguageInfoProvider = contentLanguageInfoProvider;
        }


        /// <inheritdoc/>
        public string Get()
        {
            var language = (string)httpContextAccessor.HttpContext.Request.RouteValues[DancingGoatConstants.LANGUAGE_KEY];

            if (string.IsNullOrEmpty(language))
            {
                var websiteChannel = websiteChannelInfoProvider.Get(websiteChannelContext.WebsiteChannelID);

                if (websiteChannel == null)
                {
                    throw new InvalidOperationException($"Website channel with ID {websiteChannelContext.WebsiteChannelID} does not exist.");
                }

                var languageInfo = contentLanguageInfoProvider.Get(websiteChannel.WebsiteChannelPrimaryContentLanguageID);

                if (languageInfo == null)
                {
                    throw new InvalidOperationException($"Content language with ID {websiteChannel.WebsiteChannelPrimaryContentLanguageID} does not exist.");
                }

                return languageInfo.ContentLanguageName;
            }

            return language;
        }

    }
}
