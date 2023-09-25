using DancingGoat.Models;

using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Injects DG services into the IoC container.
        /// </summary>
        public static void AddDancingGoatServices(this IServiceCollection services)
        {
            AddViewComponentServices(services);

            AddRepositories(services);

            services.AddSingleton<ICurrentLanguageRetriever, CurrentLanguageRetriever>();
            services.AddSingleton<ICurrentWebsiteChannelPrimaryLanguageRetriever, CurrentWebsiteChannelPrimaryLanguageRetriever>();
        }


        private static void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton<SocialLinkRepository>();
            services.AddSingleton<ContactRepository>();
            services.AddSingleton<HomePageRepository>();
            services.AddSingleton<ArticlePageRepository>();
            services.AddSingleton<ConfirmationPageRepository>();
            services.AddSingleton<CoffeeRepository>();
            services.AddSingleton<ImageRepository>();
            services.AddSingleton<CafeRepository>();
        }


        private static void AddViewComponentServices(IServiceCollection services)
        {
            services.AddSingleton<NavigationService>();
        }
    }
}
