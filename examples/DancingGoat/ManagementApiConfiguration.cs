#if SEPARATED_ADMIN

using Microsoft.AspNetCore.Builder;

namespace DancingGoat;

/// <summary>
/// Configures the Kentico Management API for the Dancing Goat sample site. The Management API ships with the
/// administration, so it is not available in live-site only deployments and the methods below do nothing.
/// </summary>
public static class ManagementApiConfiguration
{
    /// <summary>
    /// Does nothing; the Management API is excluded from live-site only deployments.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void AddDancingGoatManagementApi(this WebApplicationBuilder builder)
    {
    }


    /// <summary>
    /// Does nothing; the Management API is excluded from live-site only deployments.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseDancingGoatManagementApi(this WebApplication app)
    {
    }
}

#else

using Kentico.Xperience.ManagementApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DancingGoat;

/// <summary>
/// Configures the Kentico Management API for the Dancing Goat sample site. The API serves the OpenAPI specification
/// consumed by the Xperience Management MCP server configured in '.mcp.json'.
/// </summary>
public static class ManagementApiConfiguration
{
    private const string PORT_CONFIGURATION_KEY = "ManagementApi:Port";
    private const string SECRET_CONFIGURATION_KEY = "ManagementApi:Secret";


    /// <summary>
    /// Registers the Kentico Management API services in the development environment when the
    /// <c>ManagementApi:Port</c> configuration value is set.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void AddDancingGoatManagementApi(this WebApplicationBuilder builder)
    {
        if (!IsEnabled(builder.Environment, builder.Configuration))
        {
            return;
        }

        builder.Services.AddKenticoManagementApi(options => options.Secret = builder.Configuration[SECRET_CONFIGURATION_KEY]);
    }


    /// <summary>
    /// Adds the Kentico Management API middleware in the development environment when the <c>ManagementApi:Port</c>
    /// configuration value is set.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseDancingGoatManagementApi(this WebApplication app)
    {
        if (IsEnabled(app.Environment, app.Configuration))
        {
            app.UseKenticoManagementApi();
        }
    }


    // The port itself is not consumed by the application; it is the port the MCP server calls, and its presence in
    // the configuration is what enables the feature. The API is meant for local development only, so the environment
    // is checked as well.
    private static bool IsEnabled(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<int?>(PORT_CONFIGURATION_KEY).HasValue;
}

#endif
