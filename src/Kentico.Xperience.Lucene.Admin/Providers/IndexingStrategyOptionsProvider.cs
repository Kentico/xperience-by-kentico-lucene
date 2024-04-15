using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

internal class IndexingStrategyOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult(StrategyStorage.Strategies.Keys.Select(x => new DropDownOptionItem()
        {
            Value = x,
            Text = x
        }));
}
