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
    private LuceneModuleInstaller installer = null!;

    public LuceneAdminModule() : base(nameof(LuceneAdminModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("kentico", "xperience-integrations-lucene");

        var services = parameters.Services;

        installer = services.GetRequiredService<LuceneModuleInstaller>();
        storageService = services.GetRequiredService<ILuceneConfigurationStorageService>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
    }

    private void InitializeModule(object? sender, EventArgs e)
    {
        installer.Install();

        LuceneIndexStore.SetIndicies(storageService);
    }
}
