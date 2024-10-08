using CMS;
using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Scaling;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(LuceneAdminModule))]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Manages administration features and integration.
/// </summary>
internal class LuceneAdminModule : AdminModule
{
    private LuceneModuleInstaller installer = null!;
    private IWebFarmService webFarmService = null!;
    public LuceneAdminModule() : base(nameof(LuceneAdminModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("kentico", "xperience-integrations-lucene-admin");

        var services = parameters.Services;

        installer = services.GetRequiredService<LuceneModuleInstaller>();
        webFarmService = services.GetRequiredService<IWebFarmService>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
        ApplicationEvents.Initialized.Execute += RegisterLuceneWebFarmTasks;
    }

    private void InitializeModule(object? sender, EventArgs e) =>
      installer.Install();

    private void RegisterLuceneWebFarmTasks(object? sender, EventArgs e)
    {
        webFarmService.RegisterTask<IndexLogWebPageItemWebFarmTask>();
        webFarmService.RegisterTask<IndexLogReusableItemWebFarmTask>();
        webFarmService.RegisterTask<RebuildWebFarmTask>();
    }
}
