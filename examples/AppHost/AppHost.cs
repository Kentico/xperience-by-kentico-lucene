using Microsoft.Extensions.Configuration;

// Runs DancingGoat and its admin client for local development: `aspire run` from the repository root.
//
// SQL Server ("Sql:UseExistingServer" in appsettings.json, AppHost user secrets, or environment):
//   true  (default) DancingGoat uses its connection string from user secrets
//                   (scripts/Initialize-Database.ps1 or the setup-local skill)
//   false           a persistent SQL Server container; the database is initialized on first run
//
// Mailpit ("Mail:UseExistingServer", same sources):
//   true  (default) DancingGoat uses the machine-wide Mailpit at localhost:1025
//   false           a Mailpit container for this run

var builder = DistributedApplication.CreateBuilder(args);

var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));

var dancingGoat = builder.AddDotnetProject("dancinggoat", Path.Combine(repositoryRoot, "examples", "DancingGoat", "DancingGoat.csproj"))
    .WithUrls(context =>
    {
        if (context.Urls.FirstOrDefault(url => url.Endpoint?.EndpointName == "http") is { } site)
        {
            site.DisplayText = "Live site";
            context.Urls.Add(new() { Url = $"{site.Url.TrimEnd('/')}/admin", DisplayText = "Administration", Endpoint = site.Endpoint });
        }
    });

if (builder.Configuration.GetValue("Mail:UseExistingServer", true))
{
    builder.AddExternalService("mailpit", "http://localhost:8025")
        .WithHttpHealthCheck("/");
}
else
{
    var mailpit = builder.AddMailPit("mailpit");

    dancingGoat
        .WithEnvironment("SmtpOptions__Server__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
        .WithEnvironment("SmtpOptions__Server__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
        .WaitFor(mailpit);
}

if (!builder.Configuration.GetValue("Sql:UseExistingServer", true))
{
    const string databaseName = "xperience-by-kentico-lucene";

    var sql = builder.AddSqlServer("sql")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);

    var server = ReferenceExpression.Create($"{sql.Resource.PrimaryEndpoint.Property(EndpointProperty.IPV4Host)},{sql.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)}");

    var databaseInit = builder.AddExecutable("database-init", "pwsh", repositoryRoot)
        .WithArgs(context =>
        {
            context.Args.Add("-NoProfile");
            context.Args.Add("-File");
            context.Args.Add(Path.Combine("scripts", "Initialize-Database.ps1"));
            context.Args.Add("-IfNotReady");
            context.Args.Add("-SaveTo");
            context.Args.Add("Salt");
            context.Args.Add("-ServerName");
            context.Args.Add(server);
            context.Args.Add("-DatabaseName");
            context.Args.Add(databaseName);
            context.Args.Add("-Username");
            context.Args.Add("sa");

            if (builder.Configuration["XPERIENCE_LICENSE_FILE"] is { Length: > 0 } licenseFile)
            {
                context.Args.Add("-LicenseFile");
                context.Args.Add(licenseFile);
            }
        })
        .WithEnvironment("XPERIENCE_SQL_PASSWORD", sql.Resource.PasswordParameter)
        .WaitFor(sql);

    dancingGoat
        .WithEnvironment("ConnectionStrings__CMSConnectionString", ReferenceExpression.Create(
            $"Data Source={server};Initial Catalog={databaseName};User ID=sa;Password={sql.Resource.PasswordParameter};TrustServerCertificate=True;Encrypt=False"))
        .WaitForCompletion(databaseInit);
}

var adminClientPort = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(repositoryRoot, "examples", "DancingGoat", "appsettings.Development.json"))
    .Build()
    .GetSection("CMSAdminClientModuleSettings")
    .GetSection("kentico-xperience-integrations-lucene-admin")
    .GetValue<int>("Port");

var adminClient = builder.AddJavaScriptApp("admin-client", Path.Combine(repositoryRoot, "src", "Kentico.Xperience.Lucene.Admin", "Client"), "start")
    .WithNpm(install: true)
    .WithHttpEndpoint(port: adminClientPort, isProxied: false)
    .WithHttpHealthCheck("/entry.js");

dancingGoat.WaitFor(adminClient);

builder.Build().Run();
