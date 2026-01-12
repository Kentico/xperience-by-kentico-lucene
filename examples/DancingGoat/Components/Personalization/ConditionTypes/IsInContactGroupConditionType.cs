using System.Collections.Generic;
using System.Linq;

using CMS.ContactManagement;
using CMS.DataEngine;

using DancingGoat.Personalization;

using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterPersonalizationConditionType("DancingGoat.Personalization.IsInContactGroup", typeof(IsInContactGroupConditionType), "{$dancinggoat.isinccontactgroup.title$}", Description = "{$dancinggoat.isinccontactgroup.description$}", IconClass = "icon-app-contact-groups", Hint = "{$dancinggoat.isinccontactgroup.hint$}")]

namespace DancingGoat.Personalization
{
    /// <summary>
    /// Personalization condition type based on contact group.
    /// </summary>
    public class IsInContactGroupConditionType : ConditionType
    {
        /// <summary>
        /// Selected contact group code names.
        /// </summary>
        [ObjectSelectorComponent(PredefinedObjectType.CONTACTGROUP, Label = "{$dancinggoat.isinccontactgroup.contactgroups.label$}", Order = 1, MaximumItems = 0)]
        public IEnumerable<ObjectRelatedItem> SelectedContactGroups { get; set; } = Enumerable.Empty<ObjectRelatedItem>();


        /// <summary>
        /// Evaluate condition type.
        /// </summary>
        /// <returns>Returns <c>true</c> if implemented condition is met.</returns>
        public override bool Evaluate()
        {
            var contact = ContactManagementContext.GetCurrentContact();
            if (contact == null)
            {
                return false;
            }

            if (SelectedContactGroups == null || !SelectedContactGroups.Any())
            {
                return contact.ContactGroups.Count == 0;
            }

            return contact.IsInAnyContactGroup(SelectedContactGroups.Select(c => c.ObjectCodeName).ToArray());
        }
    }
}
