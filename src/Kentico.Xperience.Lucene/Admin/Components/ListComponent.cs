using CMS.DataEngine;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Admin.Components;

#pragma warning disable S2094 // intentionally empty class
public class ListComponentProperties : FormComponentProperties
{
}
#pragma warning restore

public class ListComponentClientProperties : FormComponentClientProperties<List<IncludedPath>>
{
    public List<string>? PossibleItems { get; set; }
}

public sealed class ListComponentAttribute : FormComponentAttribute
{
}

[ComponentAttribute(typeof(ListComponentAttribute))]
public class ListComponent : FormComponent<ListComponentProperties, ListComponentClientProperties, List<IncludedPath>>
{
    public List<IncludedPath>? Value { get; set; }

    public override string ClientComponentName => "@kentico/xperience-integrations-lucene/Listing";

    public override List<IncludedPath> GetValue() => Value ?? [];
    public override void SetValue(List<IncludedPath> value) => Value = value;

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
    public Task<ICommandResponse<RowActionResult>> SavePath(IncludedPath path)
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
            Value?.Add(new IncludedPath(path));
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
    }

    protected override async Task ConfigureClientProperties(ListComponentClientProperties properties)
    {
        var allWebsiteContentTypes = await DataClassInfoProvider
            .GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Website")
            .Columns(nameof(DataClassInfo.ClassName))
            .GetEnumerableTypedResultAsync();

        properties.Value = Value ?? [];
        properties.PossibleItems = allWebsiteContentTypes.Select(x => x.ClassName).ToList();

        await base.ConfigureClientProperties(properties);
    }
}
