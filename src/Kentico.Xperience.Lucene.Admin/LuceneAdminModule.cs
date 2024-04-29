using CMS;
using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(LuceneAdminModule))]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Manages administration features and integration.
/// </summary>
internal class LuceneAdminModule : AdminModule
{
    private LuceneModuleInstaller installer = null!;
    public LuceneAdminModule() : base(nameof(LuceneAdminModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("kentico", "xperience-integrations-lucene-admin");

        var services = parameters.Services;

        installer = services.GetRequiredService<LuceneModuleInstaller>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
    }

    private void InitializeModule(object? sender, EventArgs e) =>
      installer.Install();
}
