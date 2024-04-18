using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Xperience.Lucene.Admin;

internal class LanguageOptionsProvider : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider;

    public LanguageOptionsProvider(IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider) => this.contentLanguageInfoProvider = contentLanguageInfoProvider;

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        // Prepares a query for retrieving user objects
        var itemQuery = contentLanguageInfoProvider.Get();
        // If a search term is entered, only loads users users whose first name starts with the term
        if (!string.IsNullOrEmpty(searchTerm))
        {
            itemQuery.WhereStartsWith(nameof(ContentLanguageInfo.ContentLanguageDisplayName), searchTerm);
        }

        // Ensures paging of items
        itemQuery.Page(pageIndex, 20);

        // Retrieves the users and converts them into ObjectSelectorListItem<string> options
        var items = (await itemQuery.GetEnumerableTypedResultAsync()).Select(x => new ObjectSelectorListItem<string>()
        {
            Value = x.ContentLanguageName,
            Text = x.ContentLanguageDisplayName,
            IsValid = true
        });

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = itemQuery.NextPageAvailable,
            Items = items
        };
    }

    // Returns ObjectSelectorListItem<string> options for all item values that are currently selected
    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var itemQuery = contentLanguageInfoProvider.Get().Page(0, 20);
        var items = (await itemQuery.GetEnumerableTypedResultAsync()).Select(x => new ObjectSelectorListItem<string>()
        {
            Value = x.ContentLanguageName,
            Text = x.ContentLanguageDisplayName,
            IsValid = true
        });

        var selectedItems = new List<ObjectSelectorListItem<string>>();
        if (selectedValues is not null)
        {
            foreach (string? value in selectedValues)
            {
                var item = items.FirstOrDefault(x => x.Value == value);

                if (item != default)
                {
                    selectedItems.Add(item);
                }
            }
        }
        return selectedItems;
    }
}
