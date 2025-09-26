using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;

using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Globalization;
using CMS.Helpers;
using CMS.OnlineForms;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Class responsible for retrieving contact's personal data.
    /// </summary>
    internal class SampleContactDataCollectorCore
    {
        private readonly IPersonalDataWriter writer;
        private readonly IInfoProvider<ActivityInfo> activityInfoProvider;
        private readonly IInfoProvider<CountryInfo> countryInfoProvider;
        private readonly IInfoProvider<StateInfo> stateInfoProvider;
        private readonly IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;

        // Lists store Tuples of database column names and their corresponding display names.
        private readonly List<CollectedColumn> contactInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("ContactFirstName", "First name"),
            new CollectedColumn("ContactMiddleName", "Middle name"),
            new CollectedColumn("ContactLastName", "Last name"),
            new CollectedColumn("ContactJobTitle", "Job title"),
            new CollectedColumn("ContactAddress1", "Address"),
            new CollectedColumn("ContactCity", "City"),
            new CollectedColumn("ContactZIP", "ZIP"),
            new CollectedColumn("ContactStateID", ""),
            new CollectedColumn("ContactCountryID", ""),
            new CollectedColumn("ContactMobilePhone", "Mobile phone"),
            new CollectedColumn("ContactBusinessPhone", "Business phone"),
            new CollectedColumn("ContactEmail", "Email"),
            new CollectedColumn("ContactBirthday", "Birthday"),
            new CollectedColumn("ContactGender", "Gender"),
            new CollectedColumn("ContactNotes", "Notes"),
            new CollectedColumn("ContactGUID", "GUID"),
            new CollectedColumn("ContactLastModified", "Last modified"),
            new CollectedColumn("ContactCreated", "Created"),
            new CollectedColumn("ContactCampaign", "Campaign"),
            new CollectedColumn("ContactCompanyName", "Company name")
        };


        private readonly List<CollectedColumn> consentAgreementInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("ConsentAgreementGuid", "GUID"),
            new CollectedColumn("ConsentAgreementRevoked", "Consent action"),
            new CollectedColumn("ConsentAgreementTime", "Performed on")
        };


        private readonly List<CollectedColumn> consentInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("ConsentGUID", "GUID"),
            new CollectedColumn("ConsentDisplayName", "Consent name"),
            new CollectedColumn("ConsentContent", "Full text")
        };


        private readonly List<CollectedColumn> consentArchiveInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("ConsentArchiveGUID", "GUID"),
            new CollectedColumn("ConsentArchiveContent", "Full text")
        };


        private readonly List<CollectedColumn> activityInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("ActivityId", "ID"),
            new CollectedColumn("ActivityCreated", "Created"),
            new CollectedColumn("ActivityType", "Type"),
            new CollectedColumn("ActivityUrl", "URL"),
            new CollectedColumn("ActivityTitle", "Title"),
            new CollectedColumn("ActivityItemId", "")
        };


        private readonly List<CollectedColumn> countryInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("CountryDisplayName", "Country")
        };


        private readonly List<CollectedColumn> stateInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("StateDisplayName", "State")
        };


        private readonly List<CollectedColumn> contactGroupInfoColumns = new List<CollectedColumn> {
            new CollectedColumn("ContactGroupGUID", "GUID"),
            new CollectedColumn("ContactGroupName", "Contact group name"),
            new CollectedColumn("ContactGroupDescription", "Contact group description")
        };


        /// <summary>
        /// Defines form's columns containing personal data.
        /// </summary>
        private class FormDefinition
        {
            /// <summary>
            /// The form's column name that contains the user's consent agreement.
            /// </summary>
            public const string FORM_CONSENT_COLUMN_NAME = "Consent";


            /// <summary>
            /// Gets the email column name.
            /// </summary>
            public string EmailColumn { get; }


            /// <summary>
            /// Gets the code name and the display name of form fields.
            /// </summary>
            public List<CollectedColumn> FormColumns { get; }


            /// <summary>
            /// Initializes <see cref="FormDefinition"/>.
            /// </summary>
            public FormDefinition(string emailColumn, List<CollectedColumn> formColumns)
            {
                EmailColumn = emailColumn;
                FormColumns = formColumns;
            }
        }


        // Dancing Goat specific forms definitions
        // GUIDs are used to select only specific forms on the Dancing Goat sample site
        private readonly Dictionary<Guid, FormDefinition> dancingGoatForms = new Dictionary<Guid, FormDefinition>()
        {
            {
                // BusinessCustomerRegistration
                new Guid("0A5ACBBF-48B9-40DA-B431-53491588CDA7"),
                new FormDefinition(
                    "Email",
                    new List<CollectedColumn>
                    {
                        new CollectedColumn("CompanyName", "Company name"),
                        new CollectedColumn("FirstName", "First name"),
                        new CollectedColumn("LastName", "Last name"),
                        new CollectedColumn("Phone", "Phone"),
                        new CollectedColumn("BecomePartner", "Become partner"),
                        new CollectedColumn("FormInserted", "Form inserted"),
                        new CollectedColumn("FormUpdated", "Form updated")
                    }
                )
            },
            {
                // ContactUs
                new Guid("C7A6E59B-50F7-4039-ADEA-42164054E5EF"),
                new FormDefinition(
                    "UserEmail",
                    new List<CollectedColumn>
                    {
                        new CollectedColumn("UserFirstName", "First name"),
                        new CollectedColumn("UserLastName", "Last name"),
                        new CollectedColumn("UserMessage", "Message"),
                        new CollectedColumn("FormInserted", "Form inserted"),
                        new CollectedColumn("FormUpdated", "Form updated")
                    }
                )
            },
            {
                // MachineRental
                new Guid("8D0A178A-0CD1-4E95-8B37-B0B63CD28BFE"),
                new FormDefinition(
                    "Email",
                    new List<CollectedColumn>
                    {
                        new CollectedColumn("Machine", "Machine"),
                        new CollectedColumn("RentalPeriod", "Rental period"),
                        new CollectedColumn("Details", "Details"),
                        new CollectedColumn("FormInserted", "Form inserted"),
                        new CollectedColumn("FormUpdated", "Form updated")
                    }
                )
            },
            {
                // TryAFreeSample
                new Guid("4BE995DD-7675-4004-8BEF-0CF3971CBA9B"),
                new FormDefinition(
                    "EmailAddress",
                    new List<CollectedColumn>
                    {
                        new CollectedColumn("FormInserted", "Form inserted"),
                        new CollectedColumn("FormUpdated", "Form updated"),
                        new CollectedColumn("FirstName", "First name"),
                        new CollectedColumn("LastName", "Last name"),
                        new CollectedColumn("Address", "Address"),
                        new CollectedColumn("City", "City"),
                        new CollectedColumn("ZIPCode", "ZIP Code"),
                        new CollectedColumn("Country", "Country"),
                        new CollectedColumn("State", "State")
                    }
                )
            },
            // MVC Dancing Goat
            {
                // ContactUsNew MVC
                new Guid("F8C649F9-47DA-4C46-8ABF-A228D593A807"),
                new FormDefinition(
                    "UserEmail",
                    new List<CollectedColumn>
                    {
                        new CollectedColumn("UserFirstName", "First name"),
                        new CollectedColumn("UserLastName", "Last name"),
                        new CollectedColumn("UserMessage", "Message"),
                        new CollectedColumn("FormInserted", "Form inserted"),
                        new CollectedColumn("FormUpdated", "Form updated")
                    }
                )
            },
            {
                // SampleList MVC
                new Guid("E03EFE11-8C46-413E-B450-431CD1809979"),
                new FormDefinition(
                    "Email",
                    new List<CollectedColumn>
                    {
                        new CollectedColumn("FormInserted", "Form inserted"),
                        new CollectedColumn("FormUpdated", "Form updated"),
                        new CollectedColumn("FirstName", "First name"),
                        new CollectedColumn("LastName", "Last name"),
                        new CollectedColumn("Address", "Address"),
                        new CollectedColumn("City", "City"),
                        new CollectedColumn("ZIPCode", "ZIP Code"),
                        new CollectedColumn("Country", "Country"),
                        new CollectedColumn("State", "State")
                    }
                )
            }
        };


        private object TransformGenderValue(string columnName, object columnValue)
        {
            if (columnName.Equals("ContactGender", StringComparison.InvariantCultureIgnoreCase))
            {
                var gender = columnValue as int?;
                switch (gender)
                {
                    case 1:
                        return "male";
                    case 2:
                        return "female";
                    case 0:
                    default:
                        return "undefined";
                }
            }

            return columnValue;
        }


        private object TransfromConsentText(string columnName, object columnValue)
        {
            if (columnName.Equals("ConsentContent", StringComparison.InvariantCultureIgnoreCase) ||
                columnName.Equals("ConsentArchiveContent", StringComparison.InvariantCultureIgnoreCase))
            {
                var consentXml = new XmlDocument();
                consentXml.LoadXml((columnValue as string) ?? String.Empty);

                // Select the first <FullText> node
                var xmlNode = consentXml.SelectSingleNode("/ConsentContent/ConsentLanguageVersions/ConsentLanguageVersion/FullText");

                // Strip HTML tags
                var result = HTMLHelper.StripTags(xmlNode?.InnerText);

                return result;
            }

            return columnValue;
        }


        private object TransformConsentAction(string columnName, object columnValue)
        {
            if (columnName.Equals("ConsentAgreementRevoked", StringComparison.InvariantCultureIgnoreCase))
            {
                var revoked = (bool)columnValue;

                return revoked ? "Revoked" : "Agreed";
            }

            return columnValue;
        }


        /// <summary>
        /// Constructs a new instance of the <see cref="SampleContactDataCollectorCore"/>.
        /// </summary>
        /// <param name="writer">Writer to format output data.</param>
        /// <param name="activityInfoProvider">Activity info provider.</param>
        /// <param name="countryInfoProvider">Country info provider.</param>
        /// <param name="stateInfoProvider">State info provider.</param>
        /// <param name="consentAgreementInfoProvider">Consent agreement info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provider.</param>
        public SampleContactDataCollectorCore(
            IPersonalDataWriter writer,
            IInfoProvider<ActivityInfo> activityInfoProvider,
            IInfoProvider<CountryInfo> countryInfoProvider,
            IInfoProvider<StateInfo> stateInfoProvider,
            IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider)
        {
            this.writer = writer;
            this.activityInfoProvider = activityInfoProvider;
            this.countryInfoProvider = countryInfoProvider;
            this.stateInfoProvider = stateInfoProvider;
            this.consentAgreementInfoProvider = consentAgreementInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
        }


        /// <summary>
        /// Collect and format all on-line marketing related personal data about given <paramref name="identities"/>.
        /// Returns null if no data was found.
        /// </summary>
        /// <param name="identities">Identities to collect data about.</param>
        /// <returns>Formatted personal data.</returns>
        public string CollectData(IEnumerable<BaseInfo> identities)
        {
            var contacts = identities.OfType<ContactInfo>().ToList();
            if (!contacts.Any())
            {
                return null;
            }

            var contactIDs = contacts.Select(c => c.ContactID).ToList();
            var contactEmails = contacts.Select(c => c.ContactEmail).ToList();

            var contactActivities = activityInfoProvider.Get()
                                                        .Columns(activityInfoColumns.Select(t => t.Name))
                                                        .WhereIn("ActivityContactID", contactIDs).ToList();

            // Gets distinct contact groups for data subject represented by its identities
            var contactContactGroups = contacts
                .SelectMany(c => c.ContactGroups)
                .GroupBy(c => c.ContactGroupID)
                .Select(group => group.First());

            writer.WriteStartSection("OnlineMarketingData", "Online marketing data");

            WriteContacts(contacts);
            WriteConsents(contactIDs);
            WriteContactActivities(contactActivities);
            WriteContactGroups(contactContactGroups);
            WriteDancingGoatSubmittedFormsData(contactEmails, contactIDs);

            writer.WriteEndSection();

            return writer.GetResult();
        }


        /// <summary>
        /// Writes all contacts.
        /// </summary>
        /// <param name="contacts">Container of contacts.</param>
        private void WriteContacts(IEnumerable<ContactInfo> contacts)
        {
            foreach (var contactInfo in contacts)
            {
                writer.WriteStartSection(ContactInfo.OBJECT_TYPE, "Contact");
                writer.WriteBaseInfo(contactInfo, contactInfoColumns, TransformGenderValue);

                var countryID = contactInfo.ContactCountryID;
                var stateID = contactInfo.ContactStateID;
                if (countryID != 0)
                {
                    writer.WriteBaseInfo(countryInfoProvider.Get(countryID), countryInfoColumns);
                }
                if (stateID != 0)
                {
                    writer.WriteBaseInfo(stateInfoProvider.Get(stateID), stateInfoColumns);
                }

                writer.WriteEndSection();
            }
        }


        /// <summary>
        /// Writes data subject's consents.
        /// </summary>
        /// <param name="contactIDs">List of contact IDs.</param>
        private void WriteConsents(ICollection<int> contactIDs)
        {
            var consentsData = consentAgreementInfoProvider.Get()
                .Source(s => s.Join<ConsentInfo>("CMS_ConsentAgreement.ConsentAgreementConsentID", "ConsentID"))
                .Source(s => s.LeftJoin<ConsentArchiveInfo>("CMS_ConsentAgreement.ConsentAgreementConsentHash", "ConsentArchiveHash"))
                .WhereIn("ConsentAgreementContactID", contactIDs)
                .OrderBy("ConsentID")
                .OrderByDescending("ConsentAgreementTime")
                .Result;

            if (DataHelper.DataSourceIsEmpty(consentsData))
            {
                return;
            }

            // The consents and consentRevocations are specific for given consent, indexed by ConsentID
            var consents = new Dictionary<int, ConsentInfo>();
            var consentRevocations = new Dictionary<int, List<ConsentAgreementInfo>>();

            // The consentContentArchives and consentContentAgreements are specific for consent content, indexed by consent hash
            var consentContentArchives = new Dictionary<string, ConsentArchiveInfo>(StringComparer.OrdinalIgnoreCase);
            var consentContentAgreements = new Dictionary<string, List<ConsentAgreementInfo>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in consentsData.Tables[0].AsEnumerable())
            {
                var consentAgreementInfo = new ConsentAgreementInfo(row);

                ConsentInfo consentInfo;
                if (!consents.TryGetValue(consentAgreementInfo.ConsentAgreementConsentID, out consentInfo))
                {
                    consentInfo = new ConsentInfo(row);
                    consents.Add(consentAgreementInfo.ConsentAgreementConsentID, consentInfo);
                }

                if (consentAgreementInfo.ConsentAgreementRevoked)
                {
                    var revocationsOfSameConsent = GetRevocationsOfSameConsent(consentRevocations, consentAgreementInfo.ConsentAgreementConsentID);
                    revocationsOfSameConsent.Add(consentAgreementInfo);
                }
                else
                {
                    var agreementsOfSameConsentContent = GetAgreementsOfSameConsentContent(consentContentAgreements, consentAgreementInfo.ConsentAgreementConsentHash);
                    agreementsOfSameConsentContent.Add(consentAgreementInfo);

                    if (IsAgreementOfDifferentConsentContent(consentAgreementInfo, consentInfo) && !consentContentArchives.ContainsKey(consentAgreementInfo.ConsentAgreementConsentHash))
                    {
                        consentContentArchives.Add(consentAgreementInfo.ConsentAgreementConsentHash, new ConsentArchiveInfo(row));
                    }
                }
            }

            WriteConsents(consents, consentContentArchives, consentContentAgreements, consentRevocations);
        }


        /// <summary>
        /// Gets list of revocations of same consent from given <paramref name="consentRevocations"/> if presented in the dictionary.
        /// Otherwise creates new empty list and inserts it into dictionary under <paramref name="consentId"/> key.
        /// </summary>
        /// <param name="consentRevocations">Dictionary with consents revocations indexed by ConsentID.</param>
        /// <param name="consentId">Consent ID.</param>
        private static List<ConsentAgreementInfo> GetRevocationsOfSameConsent(Dictionary<int, List<ConsentAgreementInfo>> consentRevocations, int consentId)
        {
            List<ConsentAgreementInfo> revocationsOfSameConsent;
            if (!consentRevocations.TryGetValue(consentId, out revocationsOfSameConsent))
            {
                revocationsOfSameConsent = new List<ConsentAgreementInfo>();
                consentRevocations.Add(consentId, revocationsOfSameConsent);
            }

            return revocationsOfSameConsent;
        }


        /// <summary>
        /// Gets list of agreements of same consent content from given <paramref name="consentContentAgreements"/> if presented in the dictionary.
        /// Otherwise creates new empty list and inserts it into dictionary under <paramref name="consentHash"/> key.
        /// </summary>
        /// <param name="consentContentAgreements">Dictionary with consent agreements indexed by consent hash.</param>
        /// <param name="consentHash">Consent hash.</param>
        private static List<ConsentAgreementInfo> GetAgreementsOfSameConsentContent(Dictionary<string, List<ConsentAgreementInfo>> consentContentAgreements, string consentHash)
        {
            List<ConsentAgreementInfo> agreementsOfSameConsent;
            if (!consentContentAgreements.TryGetValue(consentHash, out agreementsOfSameConsent))
            {
                agreementsOfSameConsent = new List<ConsentAgreementInfo>();
                consentContentAgreements.Add(consentHash, agreementsOfSameConsent);
            }

            return agreementsOfSameConsent;
        }


        /// <summary>
        /// Returns <c>true</c> if <paramref name="consentAgreementInfo"/> is agreement of given <paramref name="consentInfo"/>, otherwise <c>false</c>.
        /// </summary>
        /// <param name="consentAgreementInfo">Consent agreement.</param>
        /// <param name="consentInfo">Consent.</param>
        private static bool IsAgreementOfDifferentConsentContent(ConsentAgreementInfo consentAgreementInfo, ConsentInfo consentInfo)
        {
            return consentAgreementInfo.ConsentAgreementConsentHash != consentInfo.ConsentHash;
        }


        /// <summary>
        /// Writes data subject's consents.
        /// </summary>
        /// <param name="consents">Dictionary with consents indexed by ConsentID.</param>
        /// <param name="consentContentArchives">Dictionary with consent archive items indexed by consent hash.</param>
        /// <param name="consentContentAgreements">Dictionary with consent agreements indexed by consent hash.</param>
        /// <param name="consentRevocations">Dictionary with consent revocation indexed by ConsentID.</param>
        private void WriteConsents(Dictionary<int, ConsentInfo> consents, Dictionary<string, ConsentArchiveInfo> consentContentArchives,
            Dictionary<string, List<ConsentAgreementInfo>> consentContentAgreements, Dictionary<int, List<ConsentAgreementInfo>> consentRevocations)
        {
            foreach (var agreementsOfSameConsentContent in consentContentAgreements.Values)
            {
                var consentAgreement = agreementsOfSameConsentContent.First();

                var consentInfo = consents[consentAgreement.ConsentAgreementConsentID];

                ConsentArchiveInfo consentArchiveInfo;
                consentContentArchives.TryGetValue(consentAgreement.ConsentAgreementConsentHash, out consentArchiveInfo);

                List<ConsentAgreementInfo> revocationsOfSameConsent;
                consentRevocations.TryGetValue(consentAgreement.ConsentAgreementConsentID, out revocationsOfSameConsent);

                WriteConsent(consentInfo, consentArchiveInfo, agreementsOfSameConsentContent, revocationsOfSameConsent);
            }
        }


        /// <summary>
        /// Writes section with Consent.
        /// </summary>
        /// <param name="consentInfo">Consent.</param>
        /// <param name="consentArchiveInfo">Consent archive. Can be null if archive item for given <paramref name="consentInfo"/> does not exist.</param>
        /// <param name="consentAgreements">Agreements of given <paramref name="consentInfo"/>.</param>
        /// <param name="consentRevocations">Revocations of given <paramref name="consentInfo"/>. Can be null if no revocations for given <paramref name="consentInfo"/> have been given.</param>
        private void WriteConsent(ConsentInfo consentInfo, ConsentArchiveInfo consentArchiveInfo, IEnumerable<ConsentAgreementInfo> consentAgreements,
            IEnumerable<ConsentAgreementInfo> consentRevocations)
        {
            writer.WriteStartSection(ConsentInfo.OBJECT_TYPE, "Consent");

            var agreedConsentLastModified = consentArchiveInfo?.ConsentArchiveLastModified ?? consentInfo.ConsentLastModified;

            WriteConsentContent(consentInfo, consentArchiveInfo);
            WriteConsentAgreements(consentAgreements);
            WriteConsentRevocations(consentRevocations?.Where(cr => cr.ConsentAgreementTime > agreedConsentLastModified));

            writer.WriteEndSection();
        }


        /// <summary>
        /// Writes consent content.
        /// </summary>
        /// <param name="consentInfo">Consent.</param>
        /// <param name="consentArchiveInfo">Consent archive. Can be null if archive item for given <paramref name="consentInfo"/> does not exist.</param>
        private void WriteConsentContent(ConsentInfo consentInfo, ConsentArchiveInfo consentArchiveInfo)
        {
            if (consentArchiveInfo == null)
            {
                writer.WriteBaseInfo(consentInfo, consentInfoColumns, TransfromConsentText);
            }
            else
            {
                writer.WriteSectionValue("ConsentDisplayName", "Consent name", consentInfo.ConsentDisplayName);
                writer.WriteBaseInfo(consentArchiveInfo, consentArchiveInfoColumns, TransfromConsentText);
            }
        }


        /// <summary>
        /// Writes consent agreements.
        /// </summary>
        /// <param name="consentAgreements">Consent agreements.</param>
        private void WriteConsentAgreements(IEnumerable<ConsentAgreementInfo> consentAgreements)
        {
            foreach (var consentAgreement in consentAgreements)
            {
                writer.WriteBaseInfo(consentAgreement, consentAgreementInfoColumns, TransformConsentAction);
            }
        }


        /// <summary>
        /// Writes consent revocations.
        /// </summary>
        /// <param name="consentRevocations">Consent revocations. Can be null if no revocations have been given.</param>
        private void WriteConsentRevocations(IEnumerable<ConsentAgreementInfo> consentRevocations)
        {
            if (consentRevocations == null)
            {
                return;
            }

            foreach (var consentRevocation in consentRevocations)
            {
                writer.WriteBaseInfo(consentRevocation, consentAgreementInfoColumns, TransformConsentAction);
            }
        }


        /// <summary>
        /// Writes all contact activities.
        /// </summary>
        /// <param name="contactActivities">List of contact activities.</param>
        private void WriteContactActivities(IEnumerable<ActivityInfo> contactActivities)
        {
            foreach (var contactActivityInfo in contactActivities)
            {
                writer.WriteStartSection(ActivityInfo.OBJECT_TYPE, "Activity");

                writer.WriteBaseInfo(contactActivityInfo, activityInfoColumns);

                writer.WriteEndSection();
            }
        }


        /// <summary>
        /// Writes all contact groups.
        /// </summary>
        /// <param name="contactContactGroups">Contact groups of a specified contact.</param>
        private void WriteContactGroups(IEnumerable<ContactGroupInfo> contactContactGroups)
        {
            foreach (var contactGroupInfo in contactContactGroups)
            {
                writer.WriteStartSection(ContactGroupInfo.OBJECT_TYPE, "Contact group");
                writer.WriteBaseInfo(contactGroupInfo, contactGroupInfoColumns);
                writer.WriteEndSection();
            }
        }


        /// <summary>
        /// Gets and writes all submitted forms with data on Dancing Goat site for specified <paramref name="emails"/> and <paramref name="contactIDs"/>.
        /// </summary>
        private void WriteDancingGoatSubmittedFormsData(ICollection<string> emails, ICollection<int> contactIDs)
        {
            var consentAgreementGuids = consentAgreementInfoProvider.Get()
                .Columns("ConsentAgreementGuid")
                .WhereIn("ConsentAgreementContactID", contactIDs);

            var formClasses = bizFormInfoProvider.Get()
                .Source(s => s.InnerJoin<DataClassInfo>("CMS_Form.FormClassID", "ClassID"))
                .WhereIn("FormGUID", dancingGoatForms.Keys);

            formClasses.ForEachRow(row =>
            {
                var bizForm = new BizFormInfo(row);
                var formClass = new DataClassInfo(row);
                var formDefinition = dancingGoatForms[bizForm.FormGUID];

                var bizFormItems = BizFormItemProvider.GetItems(formClass.ClassName)
                    .Columns(formDefinition.FormColumns.Select(t => t.Name))
                    .WhereIn(formDefinition.EmailColumn, emails);

                // Expand query if the current form contains column with consents
                if (formClass.ClassFormDefinition.Contains(FormDefinition.FORM_CONSENT_COLUMN_NAME))
                {
                    bizFormItems.Or().WhereIn(FormDefinition.FORM_CONSENT_COLUMN_NAME, consentAgreementGuids);
                }

                foreach (var bizFormItem in bizFormItems)
                {
                    writer.WriteStartSection("SubmittedForm", "Submitted form");

                    writer.WriteSectionValue("FormDisplayName", "Form display name", bizForm.FormDisplayName);
                    writer.WriteSectionValue("FormGUID", "Form GUID", bizForm.FormGUID.ToString());
                    writer.WriteBaseInfo(bizFormItem, formDefinition.FormColumns);

                    writer.WriteEndSection();
                }
            });
        }
    }
}
