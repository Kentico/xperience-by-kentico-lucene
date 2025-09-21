using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Membership.Internal;

using Kentico.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Authentication;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Admin.Components;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: RegisterFormComponent(
    identifier: LuceneSearchIndexConfigurationComponent.IDENTIFIER,
    componentType: typeof(LuceneSearchIndexConfigurationComponent),
    name: "Lucene Search Index Configuration")]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents a form component for configuring Lucene index channels and their associated properties.
/// </summary>
[ComponentAttribute(typeof(LuceneSearchIndexConfigurationComponentAttribute))]
internal sealed class LuceneSearchIndexConfigurationComponent : FormComponent<FormComponentProperties, LuceneIndexConfigurationFormComponentClientProperties, IEnumerable<LuceneIndexChannelConfiguration>>
{
    private readonly IEventLogService eventLogService;


    private readonly IInfoProvider<ChannelInfo> channelProvider;


    private readonly IApplicationPermissionEvaluator applicationPermissionEvaluator;


    private readonly IAuthenticatedUserAccessor authenticatedUserAccessor;


    private const string ApplicationNamePrefix = "Kentico.Xperience.Application.WebPages_";


    /// <summary>
    /// Represents the unique identifier for the Lucene index configuration in the Kentico Xperience integrations.
    /// </summary>
    public const string IDENTIFIER = "kentico.xperience-integrations-lucene-admin.lucene-search-index-configuration";


    private List<LuceneIndexChannelConfiguration>? Value { get; set; }


    /// <inheritdoc/>
    public override string ClientComponentName => "@kentico/xperience-integrations-lucene-admin/LuceneSearchIndexConfiguration";


    /// <inheritdoc/>
    public override IEnumerable<LuceneIndexChannelConfiguration> GetValue() => Value ?? [];


    /// <inheritdoc/>
    public override void SetValue(IEnumerable<LuceneIndexChannelConfiguration> value) => Value = value.ToList();


    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneSearchIndexConfigurationComponent"/> class.
    /// </summary>
    public LuceneSearchIndexConfigurationComponent(IInfoProvider<ChannelInfo> channelProvider,
        IApplicationPermissionEvaluator applicationPermissionEvaluator,
        IAuthenticatedUserAccessor authenticatedUserAccessor,
        IEventLogService eventLogService)
    {
        this.channelProvider = channelProvider;
        this.applicationPermissionEvaluator = applicationPermissionEvaluator;
        this.authenticatedUserAccessor = authenticatedUserAccessor;
        this.eventLogService = eventLogService;
    }


    /// <summary>
    /// Deletes the <see cref="LuceneIndexChannelConfiguration"/> with the specified channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel to be removed.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
    [FormComponentCommand]
    public async Task<ICommandResponse<RowActionSuccessResult>> DeleteWebsiteChannelConfiguration(string channelName)
    {
        var validationResult = await GetInvalidValidationResultOrNull(SystemPermissions.DELETE, channelName, nameof(DeleteWebsiteChannelConfiguration), "delete");

        if (validationResult is not null)
        {
            return validationResult;
        }

        var toRemove = Value?.Find(x => string.Equals(x.WebsiteChannelName, channelName, StringComparison.OrdinalIgnoreCase));
        if (toRemove != null)
        {
            Value?.Remove(toRemove);
        }

        return ResponseFrom(new RowActionSuccessResult(true, true));
    }


    /// <summary>
    /// Saves the specified website channel configuration by updating or adding it to the current collection.
    /// </summary>
    /// <param name="channelConfiguration">The <see cref="LuceneIndexChannelConfiguration"/> object representing the website channel configuration to save.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
    [FormComponentCommand]
    public async Task<ICommandResponse<RowActionSuccessResult>> SaveWebsiteChannelConfiguration(LuceneIndexChannelConfiguration channelConfiguration)
    {
        var validationResult = await GetInvalidValidationResultOrNull(SystemPermissions.CREATE, channelConfiguration.WebsiteChannelName, nameof(SaveWebsiteChannelConfiguration), "edit");

        if (validationResult is not null)
        {
            return validationResult;
        }

        var value = Value?.SingleOrDefault(x => string.Equals(x.WebsiteChannelName, channelConfiguration.WebsiteChannelName, StringComparison.OrdinalIgnoreCase));

        if (value is not null)
        {
            Value?.Remove(value);
        }

        Value?.Add(channelConfiguration);

        return ResponseFrom(new RowActionSuccessResult(true, true));
    }


    /// <summary>
    /// Adds a website channel configuration to the current collection if it does not already exist.
    /// </summary>
    /// <param name="websiteChannelName">The name of the website channel to add.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
    [FormComponentCommand]
    public async Task<ICommandResponse<RowActionResult>> AddWebsiteChannelConfiguration(string websiteChannelName)
    {
        var websiteChannel = channelProvider
            .Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), websiteChannelName)
            .FirstOrDefault();

        var user = await authenticatedUserAccessor.Get();

        var validationResult = GetInvalidValidationResultOrNull(SystemPermissions.CREATE, websiteChannelName, nameof(AddWebsiteChannelConfiguration), "create", websiteChannel, user);

        if (validationResult is not null)
        {
            return validationResult;
        }

        if (Value?.Exists(x => x.WebsiteChannelName == websiteChannelName) ?? false)
        {
            return ResponseFrom(new RowActionSuccessResult(false, false)).AddErrorMessage($"The specified channel: {websiteChannelName} is already configured.");
        }

        Value?.Add(new LuceneIndexChannelConfiguration(websiteChannelName, websiteChannel!.ChannelDisplayName));
        return ResponseFrom(new RowActionSuccessResult(false, true));
    }


    /// <inheritdoc/>
    protected override async Task ConfigureClientProperties(LuceneIndexConfigurationFormComponentClientProperties properties)
    {
        var allWebsiteChannels = await channelProvider
            .Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync();

        var allWebsiteContentTypes = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Website")
            .GetEnumerableTypedResult()
            .Select(x => new LuceneIndexContentType(x.ClassName, x.ClassDisplayName, 0));

        properties.Value = Value ?? [];
        properties.PossibleContentTypeItems = allWebsiteContentTypes.ToList();

        var userPermitedChannels = new List<LuceneIndexChannel>();
        var user = await authenticatedUserAccessor.Get();

        foreach (var channel in allWebsiteChannels)
        {
            if (CheckUserPermissionForChannel(channel.ChannelGUID, user, SystemPermissions.VIEW))
            {
                userPermitedChannels.Add(new LuceneIndexChannel(channel.ChannelName, channel.ChannelDisplayName));
            }
        }

        properties.PossibleChannels = userPermitedChannels;

        await base.ConfigureClientProperties(properties);
    }


    private bool CheckUserPermissionForChannel(Guid channelGuid, AdminApplicationUser user, string permission)
    {
        var applicationPermissionEvaluationContext = new ApplicationPermissionEvaluationContext
        {
            ApplicationName = ApplicationNamePrefix + channelGuid,
            User = user,
            PermissionName = permission
        };

        return applicationPermissionEvaluator.Evaluate(applicationPermissionEvaluationContext);
    }


    private async Task<ICommandResponse<RowActionSuccessResult>?> GetInvalidValidationResultOrNull(string permission, string channelName, string methodName, string action)
    {
        var channel = channelProvider
                .Get()
                .WhereEquals(nameof(ChannelInfo.ChannelName), channelName)
                .FirstOrDefault();

        var user = await authenticatedUserAccessor.Get();

        return GetInvalidValidationResultOrNull(permission, channelName, methodName, action, channel, user);
    }


    private ICommandResponse<RowActionSuccessResult>? GetInvalidValidationResultOrNull(string permission, string channelName, string methodName, string action, ChannelInfo? channel, AdminApplicationUser user)
    {
        if (channel is null)
        {
            return ResponseFrom(new RowActionSuccessResult(false, false)).AddErrorMessage($"The specified channel {channelName} does not exist.");
        }

        if (!CheckUserPermissionForChannel(channel.ChannelGUID, user, permission))
        {
            eventLogService.LogError(nameof(LuceneSearchIndexConfigurationComponent), methodName, $"A user with id: {user.UserID} tried to {action} configuration, but does not have {permission} permission. Channel name: {channelName}.");
            return ResponseFrom(new RowActionSuccessResult(false, false)).AddErrorMessage($"Current user does not have a {permission} permission for channel: {channelName}.");
        }

        return null;
    }
}
