﻿using CMS.DataEngine;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin.Components;
using Kentico.Xperience.Lucene.Models;

[assembly: RegisterFormComponent(
    identifier: LuceneIndexConfigurationComponent.IDENTIFIER,
    componentType: typeof(LuceneIndexConfigurationComponent),
    name: "Lucene Search Index Configuration")]

namespace Kentico.Xperience.Lucene.Admin.Components;

#pragma warning disable S2094 // intentionally empty class
public class LuceneIndexConfigurationComponentProperties : FormComponentProperties
{
}
#pragma warning restore

public class LuceneIndexConfigurationComponentClientProperties : FormComponentClientProperties<List<IncludedPath>>
{
    public List<string>? PossibleItems { get; set; }
}

public sealed class LuceneIndexConfigurationComponentAttribute : FormComponentAttribute
{
}

[ComponentAttribute(typeof(LuceneIndexConfigurationComponentAttribute))]
public class LuceneIndexConfigurationComponent : FormComponent<LuceneIndexConfigurationComponentProperties, LuceneIndexConfigurationComponentClientProperties, List<IncludedPath>>
{
    public const string IDENTIFIER = "kentico.xperience-integrations-lucene.lucene-index-configuration";

    internal List<IncludedPath>? Value { get; set; }

    public override string ClientComponentName => "@kentico/xperience-integrations-lucene/LuceneIndexConfiguration";

    public override List<IncludedPath> GetValue() => Value ?? new();
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

    protected override async Task ConfigureClientProperties(LuceneIndexConfigurationComponentClientProperties properties)
    {
        var allWebsiteContentTypes = await DataClassInfoProvider
            .GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Website")
            .Columns(nameof(DataClassInfo.ClassName))
            .GetEnumerableTypedResultAsync();

        properties.Value = Value ?? new();
        properties.PossibleItems = allWebsiteContentTypes.Select(x => x.ClassName).ToList();

        await base.ConfigureClientProperties(properties);
    }
}