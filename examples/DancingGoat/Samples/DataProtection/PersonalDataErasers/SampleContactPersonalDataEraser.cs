using System;
using System.Collections.Generic;
using System.Linq;

using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using CMS.OnlineForms;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IPersonalDataEraser"/> interface for erasing contact's personal data.
    /// </summary>
    internal class SampleContactPersonalDataEraser : IPersonalDataEraser
    {
        /// <summary>
        /// The form's column name that contains the user's consent agreement.
        /// </summary>
        private const string DANCING_GOAT_FORMS_CONSENT_COLUMN_NAME = "Consent";

        /// <summary>
        /// Defines forms to delete personal data from and name of the column where data subject's email be found.
        /// </summary>
        /// <remarks>
        /// GUIDs are used to select only specific forms on the Dancing Goat sample sites.
        /// </remarks>
        private readonly Dictionary<Guid, string> dancingGoatForms = new Dictionary<Guid, string>
        {
            // DancingGoatCoreContactUsNew
            { new Guid("0081DC2E-47F4-4ACD-80AE-FE39612F379C"), "UserEmail" },
            // DancingGoatCoreCoffeeSampleList
            { new Guid("DAAA080A-7B6B-489E-8150-290B1F24E715"), "Email" }
        };

        private readonly IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;
        private readonly IInfoProvider<ContactInfo> contactInfoProvider;
        private readonly IInfoProvider<ActivityInfo> activityInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleContactPersonalDataEraser"/> class.
        /// </summary>
        /// <param name="consentAgreementInfoProvider">Consent agreement info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provider.</param>
        /// <param name="contactInfoProvider">Contact info provider.</param>
        /// <param name="activityInfoProvider">Activity info provider.</param>
        public SampleContactPersonalDataEraser(
            IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider,
            IInfoProvider<ContactInfo> contactInfoProvider,
            IInfoProvider<ActivityInfo> activityInfoProvider)
        {
            this.consentAgreementInfoProvider = consentAgreementInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
            this.contactInfoProvider = contactInfoProvider;
            this.activityInfoProvider = activityInfoProvider;
        }


        /// <summary>
        /// Erases personal data based on given <paramref name="identities"/> and <paramref name="configuration"/>.
        /// </summary>
        /// <param name="identities">Collection of identities representing a data subject.</param>
        /// <param name="configuration">Configures which personal data should be erased.</param>
        /// <remarks>
        /// The erasure process can be configured via the following <paramref name="configuration"/> parameters:
        /// <list type="bullet">
        /// <item>
        /// <term>DeleteContacts</term>
        /// <description>Flag indicating whether contact(s) contained in <paramref name="identities"/> are to be deleted.</description>
        /// </item>
        /// <item>
        /// <term>DeleteActivities</term>
        /// <description>Flag indicating whether activities of contact are to be deleted.</description>
        /// </item>
        /// <item>
        /// <term>DeleteSubmittedFormsActivities</term>
        /// <description>Flag indicating whether form activities of contact are to be deleted.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public void Erase(IEnumerable<BaseInfo> identities, IDictionary<string, object> configuration)
        {
            var contacts = identities.OfType<ContactInfo>();
            if (!contacts.Any())
            {
                return;
            }

            var contactIds = contacts.Select(c => c.ContactID).ToList();
            var contactEmails = contacts.Select(c => c.ContactEmail).ToList();

            DeleteSubmittedFormsActivities(contactIds, configuration);

            DeleteActivities(contactIds, configuration);

            DeleteContacts(contacts, configuration);

            DeleteDancingGoatSubmittedFormsData(contactEmails, contactIds, configuration);
        }


        /// <summary>
        /// Deletes contact's submitted forms activities based on <paramref name="configuration"/>'s <c>DeleteSubmittedFormsActivities</c> flag.
        /// </summary>
        /// <remarks>Activities are deleted via bulk operation, considering the amount of activities for a contact.</remarks>
        private void DeleteSubmittedFormsActivities(ICollection<int> contactIds, IDictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("DeleteSubmittedFormsActivities", out object deleteSubmittedFormsActivities)
                && ValidationHelper.GetBoolean(deleteSubmittedFormsActivities, false))
            {
                activityInfoProvider.BulkDelete(new WhereCondition().WhereEquals("ActivityType", PredefinedActivityType.BIZFORM_SUBMIT)
                                                                                   .WhereIn("ActivityContactID", contactIds));
            }
        }


        /// <summary>
        /// Deletes all DancingGoat submitted forms data for <paramref name="emails"/> and <paramref name="contactIDs"/>, based on <paramref name="configuration"/>'s <c>DeleteSubmittedFormsData</c> flag.
        /// </summary>
        private void DeleteDancingGoatSubmittedFormsData(ICollection<string> emails, ICollection<int> contactIDs, IDictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("DeleteSubmittedFormsData", out object deleteSubmittedForms)
                && ValidationHelper.GetBoolean(deleteSubmittedForms, false))
            {
                var consentAgreementGuids = consentAgreementInfoProvider.Get()
                    .Columns("ConsentAgreementGuid")
                    .WhereIn("ConsentAgreementContactID", contactIDs);

                var formClasses = bizFormInfoProvider.Get()
                    .Source(s => s.LeftJoin<DataClassInfo>("CMS_Form.FormClassID", "ClassID"))
                    .WhereIn("FormGUID", dancingGoatForms.Keys);

                formClasses.ForEachRow(row =>
                {
                    var bizForm = new BizFormInfo(row);
                    var formClass = new DataClassInfo(row);
                    string emailColumn = dancingGoatForms[bizForm.FormGUID];

                    var bizFormItems = BizFormItemProvider.GetItems(formClass.ClassName)
                        .WhereIn(emailColumn, emails);

                    if (formClass.ClassFormDefinition.Contains(DANCING_GOAT_FORMS_CONSENT_COLUMN_NAME))
                    {
                        bizFormItems.Or().WhereIn(DANCING_GOAT_FORMS_CONSENT_COLUMN_NAME, consentAgreementGuids);
                    }

                    foreach (var bizFormItem in bizFormItems)
                    {
                        bizFormItem.Delete();
                    }
                });
            }
        }


        /// <summary>
        /// Deletes contact's activities based on <paramref name="configuration"/>'s <c>DeleteActivities</c> flag.
        /// </summary>
        /// <remarks>Activities are deleted via bulk operation, considering the amount of activities for a contact.</remarks>
        private void DeleteActivities(List<int> contactIds, IDictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("deleteActivities", out object deleteActivities)
                && ValidationHelper.GetBoolean(deleteActivities, false))
            {
                activityInfoProvider.BulkDelete(new WhereCondition().WhereIn("ActivityContactID", contactIds));
            }
        }


        /// <summary>
        /// Deletes <paramref name="contacts"/> based on <paramref name="configuration"/>'s <c>DeleteContacts</c> flag.
        /// </summary>
        private void DeleteContacts(IEnumerable<ContactInfo> contacts, IDictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("DeleteContacts", out object deleteContacts) && ValidationHelper.GetBoolean(deleteContacts, false))
            {
                foreach (var contact in contacts)
                {
                    contactInfoProvider.Delete(contact);
                }
            }
        }
    }
}
