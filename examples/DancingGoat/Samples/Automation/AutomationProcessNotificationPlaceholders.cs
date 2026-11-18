using CMS.Notifications;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Placeholders for the <see cref="AutomationProcessNotificationConstants.NOTIFICATION_CODE_NAME"/> notification.
    /// </summary>
    internal sealed class AutomationProcessNotificationPlaceholders : INotificationEmailPlaceholdersByCodeName
    {
        /// <inheritdoc />
        public string NotificationEmailName => AutomationProcessNotificationConstants.NOTIFICATION_CODE_NAME;


        /// <summary>
        /// Display name of the contact that is moving through the automation process.
        /// </summary>
        public string ContactName { get; init; }


        /// <summary>
        /// Display name of the automation process the contact is part of.
        /// </summary>
        public string ProcessDisplayName { get; init; }


        /// <summary>
        /// Reason explaining why the notification is being sent.
        /// </summary>
        public string NotificationReason { get; init; }
    }
}
