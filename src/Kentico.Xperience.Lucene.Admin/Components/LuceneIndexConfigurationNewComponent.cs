using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: RegisterFormComponent(
    identifier: LuceneIndexConfigurationNewComponent.IDENTIFIER,
    componentType: typeof(LuceneIndexConfigurationNewComponent),
    name: "Lucene Search Index Configuration")]

namespace Kentico.Xperience.Lucene.Admin;

#pragma warning disable S2094 // intentionally empty class
public class LuceneIndexConfigurationNewComponentProperties : FormComponentProperties
{
}
#pragma warning restore

public class LuceneIndexConfigurationNewComponentClientProperties : FormComponentClientProperties<IEnumerable<LuceneIndexChannelConfiguration>>
{
    public IEnumerable<LuceneIndexContentType>? PossibleContentTypeItems { get; set; }
    public IEnumerable<LuceneIndexChannel>? PossibleChannels { get; set; }
}

public sealed class LuceneIndexConfigurationNewComponentAttribute : FormComponentAttribute
{
}

[ComponentAttribute(typeof(LuceneIndexConfigurationNewComponentAttribute))]
public class LuceneIndexConfigurationNewComponent : FormComponent<LuceneIndexConfigurationNewComponentProperties, LuceneIndexConfigurationNewComponentClientProperties, IEnumerable<LuceneIndexChannelConfiguration>>
{
    public const string IDENTIFIER = "kentico.xperience-integrations-lucene-admin.lucene-index-new-configuration";

    internal List<LuceneIndexChannelConfiguration>? Value { get; set; }

    public override string ClientComponentName => "@kentico/xperience-integrations-lucene-admin/LuceneIndexNewConfiguration";

    public override IEnumerable<LuceneIndexChannelConfiguration> GetValue() => Value ?? [];
    public override void SetValue(IEnumerable<LuceneIndexChannelConfiguration> value) => Value = value.ToList();

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> DeleteChannelConfiguration(string channelName)
    {
        var toRemove = Value?.Find(x => Equals(x.ChannelName == channelName, StringComparison.OrdinalIgnoreCase));
        if (toRemove != null)
        {
            Value?.Remove(toRemove);
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> SaveChannelConfiguration(LuceneIndexChannelConfiguration channelConfiguration)
    {
        var value = Value?.SingleOrDefault(x => Equals(x.ChannelName == channelConfiguration.ChannelName, StringComparison.OrdinalIgnoreCase));

        if (value is not null)
        {
            Value?.Remove(value);
        }

        Value?.Add(channelConfiguration);

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> AddChannelConfiguration(string channelName)
    {
        var channel = ChannelInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), channelName)
            .FirstOrDefault();

        if (channel is null || (Value?.Exists(x => x.ChannelName == channelName) ?? false))
        {
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
        else
        {
            Value?.Add(new LuceneIndexChannelConfiguration(channelName, channel.ChannelDisplayName));
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }
    }

    protected override async Task ConfigureClientProperties(LuceneIndexConfigurationNewComponentClientProperties properties)
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
