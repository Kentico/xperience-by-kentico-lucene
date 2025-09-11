using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable S2094 // intentionally empty class
[Obsolete("The class is no longer used because the LuceneIndexConfigurationComponent is not used anymore.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
public class LuceneIndexConfigurationComponentProperties : FormComponentProperties
{
}
#pragma warning restore

[Obsolete("The class is no longer used because the LuceneIndexConfigurationComponent is not used anymore.")]
public class LuceneIndexConfigurationComponentClientProperties : FormComponentClientProperties<IEnumerable<LuceneIndexIncludedPath>>
{
    public IEnumerable<LuceneIndexContentType>? PossibleContentTypeItems { get; set; }
}

[Obsolete("The component is no longer used.")]
public class LuceneIndexConfigurationComponent : FormComponent<LuceneIndexConfigurationComponentProperties, LuceneIndexConfigurationComponentClientProperties, IEnumerable<LuceneIndexIncludedPath>>
{
    public const string IDENTIFIER = "kentico.xperience-integrations-lucene-admin.lucene-index-configuration";

    internal List<LuceneIndexIncludedPath>? Value { get; set; }

    public override string ClientComponentName => "@kentico/xperience-integrations-lucene-admin/LuceneIndexConfiguration";

    public override IEnumerable<LuceneIndexIncludedPath> GetValue() => Value ?? [];
    public override void SetValue(IEnumerable<LuceneIndexIncludedPath> value) => Value = value.ToList();

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> DeletePath(string path)
    {
        var toRemove = Value?.Find(x => Equals(x.AliasPath == path, StringComparison.OrdinalIgnoreCase));
        if (toRemove != null)
        {
            Value?.Remove(toRemove);
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> SavePath(LuceneIndexIncludedPath path)
    {
        var value = Value?.SingleOrDefault(x => Equals(x.AliasPath == path.AliasPath, StringComparison.OrdinalIgnoreCase));

        if (value is not null)
        {
            Value?.Remove(value);
        }

        Value?.Add(path);

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> AddPath(string path)
    {
        if (Value?.Exists(x => x.AliasPath == path) ?? false)
        {
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
        else
        {
            Value?.Add(new LuceneIndexIncludedPath(path));
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
    }

    protected override async Task ConfigureClientProperties(LuceneIndexConfigurationComponentClientProperties properties)
    {
        var allWebsiteContentTypes = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Website")
            .GetEnumerableTypedResult()
            .Select(x => new LuceneIndexContentType(x.ClassName, x.ClassDisplayName, 0));

        properties.Value = Value ?? [];
        properties.PossibleContentTypeItems = allWebsiteContentTypes.ToList();

        await base.ConfigureClientProperties(properties);
    }
}
