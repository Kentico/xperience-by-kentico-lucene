using System.Collections.Generic;

using CMS.Automation;
using CMS.Membership;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Properties for <see cref="SendNotificationAutomationAction"/>.
    /// </summary>
    internal sealed class SendNotificationAutomationActionProperties : IAutomationActionProperties
    {
        /// <summary>
        /// Roles whose members receive the notification email.
        /// </summary>
        [ObjectSelectorComponent(
            RoleInfo.OBJECT_TYPE,
            Label = "{$dancinggoat.automation.sendnotification.roles.label$}",
            Order = 1,
            MaximumItems = 0)]
        [RequiredValidationRule]
        public IEnumerable<ObjectRelatedItem> Roles { get; set; } = [];


        /// <summary>
        /// Reason explaining why the notification is being sent.
        /// </summary>
        [TextAreaComponent(
            Label = "{$dancinggoat.automation.sendnotification.reason.label$}",
            Order = 2)]
        [RequiredValidationRule]
        public string NotificationReason { get; set; }
    }
}
