using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: RegisterFormComponent(
    identifier: LuceneIndexConfigurationComponent.IDENTIFIER,
    componentType: typeof(LuceneIndexConfigurationComponent),
    name: "Lucene Search Index Configuration")]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents a form component for configuring Lucene index channels and their associated properties.
/// </summary>
[ComponentAttribute(typeof(LuceneIndexConfigurationComponentAttribute))]
public class LuceneIndexConfigurationComponent : FormComponent<LuceneIndexConfigurationComponentProperties, LuceneIndexConfigurationComponentClientProperties, IEnumerable<LuceneIndexChannelConfiguration>>
{
    /// <summary>
    /// Represents the unique identifier for the Lucene index configuration in the Kentico Xperience integrations.
    /// </summary>
    public const string IDENTIFIER = "kentico.xperience-integrations-lucene-admin.lucene-index-configuration";


    internal List<LuceneIndexChannelConfiguration>? Value { get; set; }


    /// <inheritdoc/>
    public override string ClientComponentName => "@kentico/xperience-integrations-lucene-admin/LuceneIndexConfiguration";


    /// <inheritdoc/>
    public override IEnumerable<LuceneIndexChannelConfiguration> GetValue() => Value ?? [];


    /// <inheritdoc/>
    public override void SetValue(IEnumerable<LuceneIndexChannelConfiguration> value) => Value = value.ToList();


    /// <summary>
    /// Deletes the <see cref="LuceneIndexChannelConfiguration"/> with the specified channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel to be removed.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
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


    /// <summary>
    /// Saves the specified website channel configuration by updating or adding it to the current collection.
    /// </summary>
    /// <param name="channelConfiguration">The <see cref="LuceneIndexChannelConfiguration"/> object representing the website channel configuration to save.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
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


    /// <summary>
    /// Adds a website channel configuration to the current collection if it does not already exist.
    /// </summary>
    /// <param name="websiteChannelName">The name of the website channel to add.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
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


    /// <inheritdoc/>
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
