using DancingGoat.Helpers;
using DancingGoat.Infrastructure;
using DancingGoat.Models;
using DancingGoat.PageTemplates;

using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat
{
    public static class IServiceCollectionExtensions
    {
        public static void AddDancingGoatServices(this IServiceCollection services)
        {
            AddViewComponentServices(services);

            AddRepositories(services);

            services.AddSingleton<RepositoryCacheHelper>();

            services.AddSingleton<MediaFileUrlHelper>();
        }

        private static void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton<ArticleRepository>();
            services.AddSingleton<CoffeeRepository>();
            services.AddSingleton<CafeRepository>();
            services.AddSingleton<ContactRepository>();
            services.AddSingleton<SocialLinkRepository>();
            services.AddSingleton<MediaFileRepository>();
            services.AddSingleton<MediaRepository>();
        }

        private static void AddViewComponentServices(IServiceCollection services)
        {
            services.AddSingleton<ArticlePageTemplateService>();
            services.AddSingleton<NavigationService>();
        }
    }
}
