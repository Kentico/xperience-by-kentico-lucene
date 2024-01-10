using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Lucene.Admin.Providers;

internal class IndexingStrategyOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult(StrategyStorage.Strategies.Keys.Select(x => new DropDownOptionItem()
        {
            Value = x,
            Text = x
        }));
}
