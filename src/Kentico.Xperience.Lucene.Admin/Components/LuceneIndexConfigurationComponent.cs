using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: RegisterFormComponent(
    identifier: LuceneIndexConfigurationComponent.IDENTIFIER,
    componentType: typeof(LuceneIndexConfigurationComponent),
    name: "Lucene Search Index Configuration")]

namespace Kentico.Xperience.Lucene.Admin;

#pragma warning disable S2094 // intentionally empty class
public class LuceneIndexConfigurationComponentProperties : FormComponentProperties
{
}
#pragma warning restore

public class LuceneIndexConfigurationComponentClientProperties : FormComponentClientProperties<IEnumerable<LuceneIndexIncludedPath>>
{
    public IEnumerable<LuceneIndexContentType>? PossibleContentTypeItems { get; set; }
}

public sealed class LuceneIndexConfigurationComponentAttribute : FormComponentAttribute
{
}

[ComponentAttribute(typeof(LuceneIndexConfigurationComponentAttribute))]
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
