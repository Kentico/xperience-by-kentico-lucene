using System;
using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.Models;

using Kentico.Activities.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Membership;
using Kentico.OnlineMarketing.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;

using Kentico.Xperience.Cloud;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddXperienceCloudApplicationInsights(builder.Configuration);

if (builder.Environment.IsQa() || builder.Environment.IsUat() || builder.Environment.IsProduction())
{
    builder.Services.AddKenticoCloud(builder.Configuration);
    builder.Services.AddXperienceCloudSendGrid(builder.Configuration);
}

builder.Services.AddKentico(features =>
{
    features.UsePageBuilder(new PageBuilderOptions
    {
        DefaultSectionIdentifier = ComponentIdentifiers.SINGLE_COLUMN_SECTION,
        RegisterDefaultSection = false,
        ContentTypeNames = new[]
        {
            LandingPage.CONTENT_TYPE_NAME,
            ContactsPage.CONTENT_TYPE_NAME,
            ArticlePage.CONTENT_TYPE_NAME
        }
    });

    features.UseWebPageRouting();
    features.UseEmailMarketing();
    features.UseEmailStatisticsLogging();
    features.UseActivityTracking();
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddLocalization()
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResources));
    });

builder.Services.AddDancingGoatServices();

ConfigureMembershipServices(builder.Services);

var app = builder.Build();

app.InitKentico();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();

if (builder.Environment.IsQa() || builder.Environment.IsUat() || builder.Environment.IsProduction())
{
    app.UseKenticoCloud();
}

app.UseKentico();

app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.Kentico().MapRoutes();

app.MapControllerRoute(
   name: "error",
   pattern: "error/{code}",
   defaults: new { controller = "HttpErrors", action = "Error" }
);

app.MapControllerRoute(
    name: DancingGoatConstants.DEFAULT_ROUTE_NAME,
    pattern: $"{{{WebPageRoutingOptions.LANGUAGE_ROUTE_VALUE_KEY}}}/{{controller}}/{{action}}",
    constraints: new
    {
        controller = DancingGoatConstants.CONSTRAINT_FOR_NON_ROUTER_PAGE_CONTROLLERS
    }
);

app.MapControllerRoute(
    name: DancingGoatConstants.DEFAULT_ROUTE_WITHOUT_LANGUAGE_PREFIX_NAME,
    pattern: "{controller}/{action}",
    constraints: new
    {
        controller = DancingGoatConstants.CONSTRAINT_FOR_NON_ROUTER_PAGE_CONTROLLERS
    }
);

app.Run();


static void ConfigureMembershipServices(IServiceCollection services)
{
    services.AddIdentity<ApplicationUser, NoOpApplicationRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 0;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredUniqueChars = 0;
        // Ensures, that disabled member cannot sign in.
        options.SignIn.RequireConfirmedAccount = true;
    })
        .AddUserStore<ApplicationUserStore<ApplicationUser>>()
        .AddRoleStore<NoOpApplicationRoleStore>()
        .AddUserManager<UserManager<ApplicationUser>>()
        .AddSignInManager<SignInManager<ApplicationUser>>();

    services.ConfigureApplicationCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.AccessDeniedPath = new PathString("/account/login");
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            var factory = ctx.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = factory.GetUrlHelper(new ActionContext(ctx.HttpContext, new RouteData(ctx.HttpContext.Request.RouteValues), new ActionDescriptor()));
            var url = urlHelper.Action("Login", "Account") + new Uri(ctx.RedirectUri).Query;

            ctx.Response.Redirect(url);

            return Task.CompletedTask;
        };
    });

    services.AddAuthorization();
}