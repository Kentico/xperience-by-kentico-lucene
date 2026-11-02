using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.Notifications;

using Microsoft.Extensions.Logging;

using Samples.DancingGoat;

[assembly: RegisterAutomationAction<SendNotificationAutomationAction>(SendNotificationAutomationAction.IDENTIFIER, "dancinggoat.automation.sendnotification.displayname", IconName = "xp-message", Description = "dancinggoat.automation.sendnotification.description")]

namespace Samples.DancingGoat
{
    /// <summary>
    /// Demo custom automation action that sends the automation process notification
    /// (<see cref="AutomationProcessNotificationConstants.NOTIFICATION_CODE_NAME"/>) to every member of the selected roles.
    /// </summary>
    internal sealed class SendNotificationAutomationAction : AutomationAction<SendNotificationAutomationActionProperties>
    {
        public const string IDENTIFIER = "DancingGoat.SendNotification";

        private readonly IRoleInfoProvider roleProvider;
        private readonly IInfoProvider<UserRoleInfo> userRoleProvider;
        private readonly INotificationEmailMessageProvider notificationEmailMessageProvider;
        private readonly IEmailService emailService;
        private readonly IProgressiveCache progressiveCache;
        private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;
        private readonly ILogger<SendNotificationAutomationAction> logger;


        /// <summary>
        /// Initializes a new instance of the <see cref="SendNotificationAutomationAction"/> class.
        /// </summary>
        public SendNotificationAutomationAction(
            IRoleInfoProvider roleProvider,
            IInfoProvider<UserRoleInfo> userRoleProvider,
            INotificationEmailMessageProvider notificationEmailMessageProvider,
            IEmailService emailService,
            IProgressiveCache progressiveCache,
            ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
            ILogger<SendNotificationAutomationAction> logger)
        {
            this.roleProvider = roleProvider;
            this.userRoleProvider = userRoleProvider;
            this.notificationEmailMessageProvider = notificationEmailMessageProvider;
            this.emailService = emailService;
            this.progressiveCache = progressiveCache;
            this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
            this.logger = logger;
        }


        /// <summary>
        /// Sends the automation process notification to every member of the roles selected on the step.
        /// </summary>
        /// <param name="properties">Step properties holding the recipient roles and the notification reason.</param>
        /// <param name="context">Context of the automation process executing the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task Execute(SendNotificationAutomationActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
        {
            var roleCodeNames = properties.Roles
                .Select(r => r.ObjectCodeName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            if (roleCodeNames.Count == 0)
            {
                logger.LogWithIntervalPolicy(
                    LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|NoRolesConfigured|{context.Process.DisplayName}", TimeSpan.FromDays(1)),
                    l => l.LogWarning("Skipping notification send — no recipient roles are configured on the step."));
                return;
            }

            var roleIds = await GetRoleIds(roleCodeNames, cancellationToken);
            if (roleIds.Count == 0)
            {
                var configuredRoles = string.Join(", ", roleCodeNames);
                logger.LogWithIntervalPolicy(
                    LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|RolesNotFound|{configuredRoles}", TimeSpan.FromDays(1)),
                    l => l.LogWarning("Skipping notification send — none of the configured roles ({Roles}) were found.", configuredRoles));
                return;
            }

            var recipientIds = await GetRoleMemberIds(roleIds, cancellationToken);
            if (recipientIds.Count == 0)
            {
                var configuredRoles = string.Join(", ", roleCodeNames);
                logger.LogWithIntervalPolicy(
                    LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|NoMembers|{configuredRoles}", TimeSpan.FromDays(1)),
                    l => l.LogInformation("Automation process notification not sent — the configured roles ({Roles}) have no members.", configuredRoles));
                return;
            }

            var placeholders = await BuildPlaceholders(properties, context, cancellationToken);
            await SendNotifications(recipientIds, placeholders, cancellationToken);
        }


        private async Task<IList<int>> GetRoleIds(IList<string> roleCodeNames, CancellationToken cancellationToken)
        {
            var cacheSettings = new CacheSettings(5, true, nameof(SendNotificationAutomationAction), nameof(GetRoleIds), string.Join("|", roleCodeNames));

            return await progressiveCache.LoadAsync(async (settings, token) =>
            {
                var result = await roleProvider
                    .Get()
                    .WhereIn(nameof(RoleInfo.RoleName), roleCodeNames)
                    .Column(nameof(RoleInfo.RoleID))
                    .GetListResultAsync<int>(cancellationToken: token);

                if (settings.Cached = result.Count > 0)
                {
                    settings.CacheDependency = cacheDependencyBuilderFactory
                        .Create()
                        .ForInfoObjects<RoleInfo>()
                        .All()
                        .Builder()
                        .Build();
                }

                return result;
            }, cacheSettings, cancellationToken);
        }


        private async Task<IList<int>> GetRoleMemberIds(IList<int> roleIds, CancellationToken cancellationToken)
        {
            var cacheSettings = new CacheSettings(5, true, nameof(SendNotificationAutomationAction), nameof(GetRoleMemberIds), string.Join("|", roleIds.OrderBy(id => id)));

            return await progressiveCache.LoadAsync(async (settings, token) =>
            {
                var userIds = await userRoleProvider
                    .Get()
                    .WhereIn(nameof(UserRoleInfo.RoleID), roleIds)
                    .Column(nameof(UserRoleInfo.UserID))
                    .GetListResultAsync<int>(cancellationToken: token);

                var result = userIds.Distinct().ToList();

                if (settings.Cached = result.Count > 0)
                {
                    settings.CacheDependency = cacheDependencyBuilderFactory
                        .Create()
                        .ForInfoObjects<UserRoleInfo>()
                        .All()
                        .Builder()
                        .Build();
                }

                return result;
            }, cacheSettings, cancellationToken);
        }


        private static async Task<AutomationProcessNotificationPlaceholders> BuildPlaceholders(SendNotificationAutomationActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
        {
            var contact = await context.GetProcessedObject(cancellationToken);

            return new AutomationProcessNotificationPlaceholders
            {
                ContactName = contact.ContactDescriptiveName,
                ProcessDisplayName = context.Process.DisplayName,
                NotificationReason = properties.NotificationReason
            };
        }


        private async Task SendNotifications(IList<int> recipientIds, AutomationProcessNotificationPlaceholders placeholders, CancellationToken cancellationToken)
        {
            foreach (var recipientId in recipientIds)
            {
                try
                {
                    var emailMessage = await notificationEmailMessageProvider.CreateEmailMessage(AutomationProcessNotificationConstants.NOTIFICATION_CODE_NAME, recipientId, placeholders, cancellationToken);
                    await emailService.SendEmail(emailMessage);
                }
                catch (Exception ex)
                {
                    logger.LogWithIntervalPolicy(
                        LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|SendFailed|{recipientId}", TimeSpan.FromDays(1)),
                        l => l.LogError(ex, "Failed to send automation process notification to user {UserId}.", recipientId));
                }
            }
        }
    }
}
