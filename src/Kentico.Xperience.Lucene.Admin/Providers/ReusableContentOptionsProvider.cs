using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Xperience.Lucene.Admin.Providers;
internal class ReusableContentOptionsProvider : IGeneralSelectorDataProvider
{
    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        // Prepares a query for retrieving objects
        var itemQuery = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Reusable");
        // If a search term is entered, only loads data whose first name starts with the term
        if (!string.IsNullOrEmpty(searchTerm))
        {
            itemQuery.WhereStartsWith(nameof(DataClassInfo.ClassDisplayName), searchTerm);
        }

        // Ensures paging of items
        itemQuery.Page(pageIndex, 20);

        // Retrieves the reusable content types and converts them into ObjectSelectorListItem<string> options
        var items = (await itemQuery.GetEnumerableTypedResultAsync()).Select(x => new ObjectSelectorListItem<string>()
        {
            Value = x.ClassName,
            Text = x.ClassDisplayName,
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
        var itemQuery = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Reusable");

        var items = (await itemQuery.GetEnumerableTypedResultAsync()).Select(x => new ObjectSelectorListItem<string>()
        {
            Value = x.ClassName,
            Text = x.ClassDisplayName,
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
