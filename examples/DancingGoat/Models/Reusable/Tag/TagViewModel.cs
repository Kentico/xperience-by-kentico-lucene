namespace DancingGoat.Models;

public record TagViewModel(string Name, int Level, Guid Value, bool IsChecked = false)
{
    private const int ROOT_TAG_ID = 0;

    public static TagViewModel GetViewModel(CMS.ContentEngine.Tag tag, int level = 0) => new(tag.Title, level, tag.Identifier);


    public static List<TagViewModel> GetViewModels(IEnumerable<CMS.ContentEngine.Tag> tags)
    {
        var result = new List<TagViewModel>();
        var tagsByParentId = tags.GroupBy(tag => tag.ParentID).ToDictionary(group => group.Key, group => group.ToList());

        if (tagsByParentId.TryGetValue(ROOT_TAG_ID, out var firstLevelTags))
        {
            GetTagsWithTagViewModels(firstLevelTags, ROOT_TAG_ID);
        }

        return result;


        void GetTagsWithTagViewModels(IEnumerable<CMS.ContentEngine.Tag> currentLevelTags, int level)
        {
            foreach (var tag in currentLevelTags.OrderBy(tag => tag.Order))
            {
                var children = tagsByParentId.TryGetValue(tag.ID, out var childrenTags) ? childrenTags : Enumerable.Empty<CMS.ContentEngine.Tag>();
                result.Add(GetViewModel(tag, level));
                GetTagsWithTagViewModels(children, level + 1);
            }
        }
    }
}
