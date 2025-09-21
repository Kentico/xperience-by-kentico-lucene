using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.EmailComponents;
using DancingGoat.Helpers.Generators;
using DancingGoat.Models;
using DancingGoat.Search;

using CMS;
using CMS.Base;

using Kentico.Activities.Web.Mvc;
using Kentico.Commerce.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Membership;
using Kentico.OnlineMarketing.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Mjml;
using Kentico.Web.Mvc;


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Samples.DancingGoat;

[assembly: AssemblyDiscoverable]

var builder = WebApplication.CreateBuilder(args);


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

    features.UseEmailBuilder();
    features.UseWebPageRouting();
    features.UseEmailMarketing();
    features.UseEmailStatisticsLogging();
    features.UseActivityTracking();
    features.UseCommerce();
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddLocalization()
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResources)));

builder.Services.AddDancingGoatServices();

builder.Services.AddKenticoDancingGoatLuceneServices(builder.Configuration);

builder.Services.AddSingleton<IEmailActivityTrackingEvaluator, EmailActivityTrackingEvaluator>();

ConfigureEmailBuilder(builder.Services);
ConfigureMembershipServices(builder.Services);

if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<UrlResolveOptions>(options => options.UseSSL = false);
}

var app = builder.Build();

app.InitKentico();

app.InitializeDancingGoat();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();


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

    services.Configure<AdminIdentityOptions>(options =>
    {
        // The expiration time span of 8 hours is set for demo purposes only. In production environments, set expiration according to best practices.
        options.AuthenticationOptions.ExpireTimeSpan = TimeSpan.FromHours(8);

        // The forbidden passwords are set for demo purposes only. In production environments, set password options according to best practices.
        var companySpecificKeywords = new List<string> { "kentico", "dancinggoat", "admin", "coffee" };
        var specificNumberCombinations = new List<string> { "2023", "23", "2024", "24", "2025", "25" };
        options.PasswordOptions.ForbiddenPasswords = ForbiddenPasswordGenerator.Generate(companySpecificKeywords, specificNumberCombinations);
    });

    services.AddAuthorization();
}


static void ConfigureEmailBuilder(IServiceCollection services)
{
    services.Configure((EmailBuilderOptions options) =>
    {
        options.AllowedEmailContentTypeNames = ["DancingGoat.BuilderEmail"];
        options.RegisterDefaultSection = false;
        options.DefaultSectionIdentifier = DancingGoatFullWidthEmailSection.IDENTIFIER;
    });

    services.AddMjmlForEmails();
}
