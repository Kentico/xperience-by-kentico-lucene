using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin.Providers;

internal class AnalyzerOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
       Task.FromResult(AnalyzerStorage.Analyzers.Keys.Select(x => new DropDownOptionItem()
       {
           Value = x,
           Text = x
       }));
}
