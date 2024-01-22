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
    private ILuceneConfigurationStorageService? storageService;
    private LuceneModuleInstaller? installer;
    private LuceneModuleMigrator? migrator;


    public LuceneAdminModule()
        : base(nameof(LuceneAdminModule))
    {
    }


    /// <inheritdoc/>
    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("kentico", "xperience-integrations-lucene");

        var services = parameters.Services;

        installer = services.GetRequiredService<LuceneModuleInstaller>();
        migrator = services.GetRequiredService<LuceneModuleMigrator>();
        storageService = services.GetRequiredService<ILuceneConfigurationStorageService>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
        ApplicationEvents.ExecuteMigrations.Execute += ExecuteMigrations;
    }


    private void InitializeModule(object? sender, EventArgs e)
    {
        installer?.Install();

        if (storageService is not null)
        {
            LuceneIndexStore.SetIndicies(storageService);
        }
    }

    private void ExecuteMigrations(object? sender, MigrationExecutionEventArgs e) => migrator?.Migrate(e);
}
