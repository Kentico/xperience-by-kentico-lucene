using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.Base;
using CMS.ContactManagement;
using CMS.EmailLibrary;
using CMS.EmailMarketing;
using CMS.Helpers;

namespace DancingGoat.Helpers.Generator
{
    /// <summary>
    /// Generates emails with real-like dependencies.
    /// </summary>
    public class EmailLibrarySampleGenerator
    {
        private const string GeneratedLabel = "Generated";
        private const string EmptyHash = "0000000000000000000000000000000000000000000000000000000000000000";
        private const string SenderAddress = $"{nameof(EmailLibrarySampleGenerator)}@localhost.local";

        public const string GeneratedInfoCodeNameSuffix = "273efa91-79e0-4b2c-8cc4-f868cd9f9f54";
        private readonly ISiteInfo site;


        /// <summary>
        /// Creates a new instance of <see cref="EmailLibrarySampleGenerator"/>.
        /// </summary>
        /// <param name="site">A site the sample belongs to.</param>
        public EmailLibrarySampleGenerator(ISiteInfo site)
        {
            this.site = site;
        }


        /// <summary>
        /// Gets or creates the given number of contacts.
        /// </summary>
        /// <param name="quantity">The number of contacts to generate.</param>
        public List<ContactInfo> GenerateContacts(int quantity)
        {
            var contacts = new List<ContactInfo>();

            for (var i = 0; i < quantity; i++)
            {
                var firstName = $"Name-{i}";
                var lastName = $"Surname-{i}";
                var email = $"{firstName}.{GeneratedLabel}.{lastName}@localhost.local";

                var contact = ContactInfoProvider.GetContactInfo(email);

                if (contact is null)
                {
                    contact = new ContactInfo
                    {
                        ContactEmail = email,
                        ContactFirstName = firstName,
                        ContactLastName = lastName,
                        ContactMiddleName = GeneratedLabel
                    };

                    ContactInfo.Provider.Set(contact);
                }

                contacts.Add(contact);
            }

            return contacts;
        }


        /// <summary>
        /// Gets or creates a recipient list.
        /// </summary>
        /// <param name="subscribeConfiguration">The configuration used to generate subscription confirmation email.</param>
        /// <param name="subscribePage">The page to link in the subscription confirmation email.</param>
        /// <param name="unsubscribeConfiguration">The configuration used to generate unsubscription confirmation email.</param>
        /// <param name="unsubscribePage">The page to link in the unsubscription confirmation email.</param>
        public async Task<ContactGroupInfo> GenerateRecipientListAsync(EmailConfigurationInfo subscribeConfiguration = null,
            Guid? subscribePage = null, EmailConfigurationInfo unsubscribeConfiguration = null, Guid? unsubscribePage = null)
        {
            var (displayName, codeName) = GetNames("Generated Recipient List");

            var info = await ContactGroupInfo.Provider.GetAsync(codeName);
            if (info is null)
            {
                info = new ContactGroupInfo
                {
                    ContactGroupDisplayName = displayName,
                    ContactGroupDescription = GeneratedLabel,
                    ContactGroupName = codeName,
                    ContactGroupEnabled = true,
                    ContactGroupStatus = ContactGroupStatusEnum.Ready,
                    ContactGroupIsRecipientList = true
                };

                info.Insert();
            }

            GenerateRecipientListSettings(info, subscribeConfiguration, subscribePage, unsubscribeConfiguration, unsubscribePage);
            return info;
        }


        /// <summary>
        /// Adds given contacts to the given recipient list and generates appropriate subscription confirmation records.
        /// </summary>
        /// <param name="contacts">Email recipients.</param>
        /// <param name="recipientList">A recipient list the contacts should be listed in.</param>
        public void AddContactsToRecipientList(IEnumerable<ContactInfo> contacts, ContactGroupInfo recipientList)
        {
            foreach (var contact in contacts)
            {
                GenerateSubscriptionConfirmation(contact, recipientList);
                EnsureRecipientListMember(contact, recipientList);
            }
        }

        /// <summary>
        /// Get or creates an email template.
        /// </summary>
        /// <param name="type">The type of the email.</param>
        /// <returns></returns>
        public async Task<EmailTemplateInfo> GenerateTemplateAsync(EmailType type)
        {
            var (displayName, codeName) = GetNames(type.ToString());

            var template = await EmailTemplateInfoProvider.ProviderObject.GetAsync(codeName, site.SiteID);
            if (template is null)
            {
                var templateCode = type switch
                {
                    EmailType.Regular => "<div>$$content$$</div><div>$$unsubscribeurl$$</div>",
                    _ => "<div>$$content$$</div>"
                };

                template = new EmailTemplateInfo
                {
                    EmailTemplateName = codeName,
                    EmailTemplateDisplayName = displayName,
                    EmailTemplateDescription = "A generated template to give a sample of email statistics.",
                    EmailTemplateCode = $"<html><body>{templateCode}</body></html>",
                    EmailTemplateSiteID = site.SiteID,
                    EmailTemplateType = type
                };

                template.Insert();
            }

            return template;
        }


        /// <summary>
        /// Gets or creates email configuration.
        /// </summary>
        /// <param name="name">A display name. Serves as a base for code name generation.</param>
        /// <param name="template">The email template.</param>
        public async Task<EmailConfigurationInfo> GenerateConfigurationAsync(EmailTemplateInfo template, string name = null)
        {
            var (displayName, codeName) = GetNames(name ?? template.EmailTemplateType.ToString());

            var configuration = await EmailConfigurationInfo.Provider.GetAsync(codeName, site.SiteID);
            if (configuration is null)
            {
                configuration = new EmailConfigurationInfo
                {
                    EmailConfigurationName = codeName,
                    EmailConfigurationDisplayName = displayName,
                    EmailConfigurationSubject = "This is a generated email.",
                    EmailConfigurationSenderName = nameof(EmailLibrarySampleGenerator),
                    EmailConfigurationSenderEmail = SenderAddress,
                    EmailConfigurationEmailTemplateID = template.EmailTemplateID,
                    EmailConfigurationContent = GeneratedLabel,
                    EmailConfigurationSiteID = site.SiteID,
                    EmailConfigurationPlainText = "",
                    EmailConfigurationType = template.EmailTemplateType
                };

                configuration.Insert();
            }

            return configuration;
        }


        /// <summary>
        /// Creates email statistics hits.
        /// </summary>
        /// <param name="emailsToOpen">The emails that should be recorded as opened.</param>
        /// <param name="emailsToClick">The emails that should be recorded as clicked.</param>
        public void GenerateEmailStatisticsHits(IEnumerable<SampleEmailContext> emailsToOpen = null,
            IEnumerable<SampleEmailContext> emailsToClick = null)
        {
            if (emailsToOpen is not null)
            {
                GenerateEmailStatisticsHits(EmailStatisticsHitsType.Open, emailsToOpen);
            }

            if (emailsToClick is not null)
            {
                GenerateEmailStatisticsHits(EmailStatisticsHitsType.Click, emailsToClick);
            }
        }


        /// <summary>
        /// Simulate sending emails to given contacts and generates related sent hits.
        /// </summary>
        /// <param name="configuration">The configuration to send.</param>
        /// <param name="contacts">Email recipients.</param>
        /// <param name="recipientList">Recipient list.</param>
        public async Task<SampleEmailContext[]> SimulateEmailSend(EmailConfigurationInfo configuration, IEnumerable<ContactInfo> contacts,
            ContactGroupInfo recipientList)
        {
            if (configuration.EmailConfigurationType == EmailType.Regular)
            {
                await GenerateSendConfigurationAsync(configuration, recipientList);
            }

            var emails = contacts
                .Select(contact => new SampleEmailContext(configuration, recipientList, contact))
                .ToArray();
            InjectLinks(configuration, emails);
            GenerateEmailStatisticsHits(EmailStatisticsHitsType.Sent, emails);
            return emails;
        }


        /// <summary>
        /// Marks e-mail addresses of given contacts as soft-bounced with number of bounces in interval 1-5 
        /// and generates corresponding hits with <see cref="EmailStatisticsHitsType.SoftBounce"/> hit type.
        /// </summary>
        /// <param name="configuration">The configuration to send.</param>
        /// <param name="contacts">Mail recipients to be marked as soft-bounced.</param>
        public void GenerateSoftEmailBounces(EmailConfigurationInfo configuration, IEnumerable<ContactInfo> contacts)
        {
            var emails = new List<SampleEmailContext>();
            var bounceCounter = 0;
            foreach (var contact in contacts)
            {
                emails.Add(new SampleEmailContext(configuration, null, contact));

                var contactBounceCount = 1 + (bounceCounter % 5);

                var emailBounce = EmailBounceInfo.Provider.Get()
                       .WhereEquals(nameof(EmailBounceInfo.EmailBounceEmailAddress), contact.ContactEmail)
                       .TopN(1)
                       .FirstOrDefault();

                if (emailBounce is null)
                {
                    emailBounce = new EmailBounceInfo
                    {
                        EmailBounceEmailAddress = contact.ContactEmail,
                        EmailBounceIsHardBounce = false,
                        EmailBounceSoftBounceCount = contactBounceCount
                    };
                }
                else
                {
                    emailBounce.EmailBounceIsHardBounce = false;
                    emailBounce.EmailBounceSoftBounceCount = contactBounceCount;
                }

                EmailBounceInfo.Provider.Set(emailBounce);

                bounceCounter++;
            }

            GenerateEmailStatisticsHits(EmailStatisticsHitsType.SoftBounce, emails);
        }


        /// <summary>
        /// Marks e-mail addresses of given contacts as hard-bounced and generates corresponding hits with <see cref="EmailStatisticsHitsType.HardBounce"/> hit type.
        /// </summary>
        /// <param name="configuration">The configuration to send.</param>
        /// <param name="contacts">Mail recipients to be marked as hard-bounced.</param>
        public void GenerateHardEmailBounces(EmailConfigurationInfo configuration, IEnumerable<ContactInfo> contacts)
        {
            var emails = new List<SampleEmailContext>();
            foreach (var contact in contacts)
            {
                emails.Add(new SampleEmailContext(configuration, null, contact));

                var emailBounce = EmailBounceInfo.Provider.Get()
                        .WhereEquals(nameof(EmailBounceInfo.EmailBounceEmailAddress), contact.ContactEmail)
                        .TopN(1)
                        .FirstOrDefault();

                if (emailBounce is null)
                {
                    emailBounce = new EmailBounceInfo
                    {
                        EmailBounceEmailAddress = contact.ContactEmail,
                        EmailBounceIsHardBounce = true,
                        EmailBounceSoftBounceCount = 0
                    };
                }
                else
                {
                    emailBounce.EmailBounceIsHardBounce = true;
                    emailBounce.EmailBounceSoftBounceCount = 0;
                }

                EmailBounceInfo.Provider.Set(emailBounce);
            }

            GenerateEmailStatisticsHits(EmailStatisticsHitsType.HardBounce, emails);
        }


        private static (string, string) GetNames(string value)
        {
            if (!value.StartsWith(GeneratedLabel))
            {
                value = $"{GeneratedLabel} {value}";
            }

            var codeName = ValidationHelper.GetCodeName(
                value.EndsWith(GeneratedInfoCodeNameSuffix) ? value : $"{value}_{GeneratedInfoCodeNameSuffix}");

            return (value, codeName);
        }


        private static async Task GenerateSendConfigurationAsync(EmailConfigurationInfo configuration,
            ContactGroupInfo recipientList)
        {
            var info = await SendConfigurationInfo.Provider.GetByEmailConfigurationIdAsync(configuration.EmailConfigurationID);
            if (info is null)
            {
                new SendConfigurationInfo
                {
                    SendConfigurationEmailConfigurationID = configuration.EmailConfigurationID,
                    SendConfigurationRecipientListID = recipientList.ContactGroupID,
                    SendConfigurationScheduledTime = DateTime.Now - TimeSpan.FromHours(1),
                    SendConfigurationStatus = SendConfigurationStatus.Sent
                }.Insert();
            }
        }


        private static void InjectLinks(EmailConfigurationInfo configuration, IEnumerable<SampleEmailContext> emails)
        {
            var infos = EmailLinkInfo.Provider.Get()
                .WhereEquals(nameof(EmailLinkInfo.EmailLinkEmailConfigurationID), configuration.EmailConfigurationID)
                .WhereEquals(nameof(EmailLinkInfo.EmailLinkDescription), GeneratedLabel)
                .ToArray();

            foreach (var email in emails)
            {
                var target = configuration.EmailConfigurationType switch
                {
                    EmailType.FormAutoresponder => $"~/Kentico.Emails/{configuration.EmailConfigurationName}?contactEmail={email.Contact.ContactEmail}&recipientListID={email.RecipientList.ContactGroupID}&hash={EmptyHash}",
                    EmailType.Regular => $"~/Kentico.Redirect/?href=localhost.local%2Fhome&contactEmail={email.Contact.ContactEmail}&hash={EmptyHash}",
                    _ => null
                };

                if (target is null)
                {
                    continue;
                }

                var link = Array.Find(infos, x => x.EmailLinkTarget == target);
                if (link is null)
                {
                    link = new EmailLinkInfo
                    {
                        EmailLinkEmailConfigurationID = configuration.EmailConfigurationID,
                        EmailLinkTarget = target,
                        EmailLinkDescription = GeneratedLabel,
                    };

                    link.Insert();
                }

                email.Links.Add(link);
            }
        }


        private static void GenerateEmailStatisticsHits(EmailStatisticsHitsType type, IEnumerable<SampleEmailContext> emails)
        {
            foreach (var email in emails)
            {
                var hit = new EmailStatisticsHitsInfo
                {
                    EmailStatisticsHitsEmailConfigurationID = email.Configuration.EmailConfigurationID,
                    EmailStatisticsHitsType = type,
                    EmailStatisticsHitsTime = DateTime.Now,
                    EmailStatisticsHitsMailoutGUID = email.EmailGuid
                };

                var linkId = email.Links.FirstOrDefault()?.EmailLinkID;
                if (linkId.HasValue)
                {
                    hit.EmailStatisticsHitsEmailLinkID = linkId.Value;
                }

                hit.Insert();
            }
        }


        private static void GenerateSubscriptionConfirmation(ContactInfo contact, ContactGroupInfo recipientList)
        {
            var subscriptionConfirmation = EmailSubscriptionConfirmationInfo.Provider.Get()
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), recipientList.ContactGroupID)
                .TopN(1)
                .FirstOrDefault();

            if (subscriptionConfirmation is null)
            {
                new EmailSubscriptionConfirmationInfo
                {
                    EmailSubscriptionConfirmationContactID = contact.ContactID,
                    EmailSubscriptionConfirmationRecipientListID = recipientList.ContactGroupID,
                    EmailSubscriptionConfirmationDate = DateTime.Today,
                    EmailSubscriptionConfirmationIsApproved = true,
                }.Insert();
            }
        }


        private static void EnsureRecipientListMember(ContactInfo contact, ContactGroupInfo recipientList)
        {
            var info = ContactGroupMemberInfo.Provider.Get(recipientList.ContactGroupID, contact.ContactID,
                ContactGroupMemberTypeEnum.Contact);

            if (info is null)
            {
                info = new ContactGroupMemberInfo
                {
                    ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
                    ContactGroupMemberContactGroupID = recipientList.ContactGroupID,
                    ContactGroupMemberRelatedID = contact.ContactID,
                    ContactGroupMemberFromManual = true
                };

                info.Insert();
            }
        }


        private void GenerateRecipientListSettings(ContactGroupInfo recipientList, EmailConfigurationInfo subscribeConfiguration = null,
            Guid? subscribePage = null, EmailConfigurationInfo unsubscribeConfiguration = null, Guid? unsubscribePage = null)
        {
            var info = RecipientListSettingsInfo.Provider.Get()
                .WhereEquals(nameof(RecipientListSettingsInfo.RecipientListSettingsRecipientListID), recipientList.ContactGroupID)
                .TopN(1)
                .FirstOrDefault();

            if (info is null)
            {
                info = new RecipientListSettingsInfo
                {
                    RecipientListSettingsRecipientListID = recipientList.ContactGroupID,
                    RecipientListSettingsSendSubscriptionConfirmationEmail = false,
                    RecipientListSettingsSendUnsubscriptionConfirmationEmail = false
                };

                if (subscribeConfiguration is not null)
                {
                    info.RecipientListSettingsSendSubscriptionConfirmationEmail = true;
                    info.RecipientListSettingsSubscriptionConfirmationEmailID = subscribeConfiguration.EmailConfigurationID;
                    if (subscribePage.HasValue)
                    {
                        info.RecipientListSettingsAfterConfirmationPage = subscribePage.Value;
                    }
                }

                if (unsubscribeConfiguration is not null)
                {
                    info.RecipientListSettingsSendUnsubscriptionConfirmationEmail = true;
                    info.RecipientListSettingsUnsubscriptionConfirmationEmailID = unsubscribeConfiguration.EmailConfigurationID;
                    if (unsubscribePage.HasValue)
                    {
                        info.RecipientListSettingsAfterUnsubscriptionPage = unsubscribePage.Value;
                    }
                }

                info.Insert();
            }
        }
    }
}
