using CMS;
using CMS.Base;
using CMS.Core;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(LuceneAdminModule))]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Manages administration features and integration.
/// </summary>
internal class LuceneAdminModule : AdminModule
{
    private ILuceneConfigurationStorageService storageService = null!;
    private LuceneModuleMigrator migrator = null!;

    public LuceneAdminModule() : base(nameof(LuceneAdminModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("kentico", "xperience-integrations-lucene");

        var services = parameters.Services;

        migrator = services.GetRequiredService<LuceneModuleMigrator>();
        storageService = services.GetRequiredService<ILuceneConfigurationStorageService>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
        ApplicationEvents.ExecuteMigrations.Execute += ExecuteMigrations;
    }

    private void InitializeModule(object? sender, EventArgs e)
    {
        string[] args = Environment.GetCommandLineArgs();

        // Workaround to ensure we don't perform checks or setup the module
        // when running an update
        if (args is null || !Array.Exists(args, a => a.Equals("--kxp-update")))
        {
            migrator.ValidateModuleInstall();

            LuceneIndexStore.SetIndicies(storageService);
        }
    }

    private void ExecuteMigrations(object? sender, MigrationExecutionEventArgs e) => migrator.Migrate();
}
