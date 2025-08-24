using CMS.ContentEngine;
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

public class LuceneIndexConfigurationComponentClientProperties : FormComponentClientProperties<IEnumerable<LuceneIndexChannelConfiguration>>
{
    public IEnumerable<LuceneIndexContentType>? PossibleContentTypeItems { get; set; }
    public IEnumerable<LuceneIndexChannel>? PossibleChannels { get; set; }
}

public sealed class LuceneIndexConfigurationComponentAttribute : FormComponentAttribute
{
}

[ComponentAttribute(typeof(LuceneIndexConfigurationComponentAttribute))]
public class LuceneIndexConfigurationComponent : FormComponent<LuceneIndexConfigurationComponentProperties, LuceneIndexConfigurationComponentClientProperties, IEnumerable<LuceneIndexChannelConfiguration>>
{
    public const string IDENTIFIER = "kentico.xperience-integrations-lucene-admin.lucene-index-configuration";

    internal List<LuceneIndexChannelConfiguration>? Value { get; set; }

    public override string ClientComponentName => "@kentico/xperience-integrations-lucene-admin/LuceneIndexConfiguration";

    public override IEnumerable<LuceneIndexChannelConfiguration> GetValue() => Value ?? [];
    public override void SetValue(IEnumerable<LuceneIndexChannelConfiguration> value) => Value = value.ToList();

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> DeleteWebsiteChannelConfiguration(string channelName)
    {
        var toRemove = Value?.Find(x => Equals(x.WebsiteChannelName == channelName, StringComparison.OrdinalIgnoreCase));
        if (toRemove != null)
        {
            Value?.Remove(toRemove);
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> SaveWebsiteChannelConfiguration(LuceneIndexChannelConfiguration channelConfiguration)
    {
        var value = Value?.SingleOrDefault(x => Equals(x.WebsiteChannelName == channelConfiguration.WebsiteChannelName, StringComparison.OrdinalIgnoreCase));

        if (value is not null)
        {
            Value?.Remove(value);
        }

        Value?.Add(channelConfiguration);

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> AddWebsiteChannelConfiguration(string websiteChannelName)
    {
        var websiteChannel = ChannelInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), websiteChannelName)
            .FirstOrDefault();

        if (websiteChannel is null || (Value?.Exists(x => x.WebsiteChannelName == websiteChannelName) ?? false))
        {
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
        else
        {
            Value?.Add(new LuceneIndexChannelConfiguration(websiteChannelName, websiteChannel.ChannelDisplayName));
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
    }

    protected override async Task ConfigureClientProperties(LuceneIndexConfigurationComponentClientProperties properties)
    {
        var allWebsiteChannels = (await ChannelInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync())
            .Select(x => new LuceneIndexChannel(x.ChannelName, x.ChannelDisplayName));

        var allWebsiteContentTypes = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Website")
            .GetEnumerableTypedResult()
            .Select(x => new LuceneIndexContentType(x.ClassName, x.ClassDisplayName, 0));

        properties.Value = Value ?? [];
        properties.PossibleContentTypeItems = allWebsiteContentTypes.ToList();
        properties.PossibleChannels = allWebsiteChannels.ToList();

        await base.ConfigureClientProperties(properties);
    }
}
