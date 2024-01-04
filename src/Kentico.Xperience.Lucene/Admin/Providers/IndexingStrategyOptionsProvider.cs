using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Lucene.Admin.Providers;

public class IndexingStrategyOptionsProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        StrategyStorage.Strategies.Keys.Select(x => new DropDownOptionItem()
        {
            Value = x,
            Text = x
        });
}
