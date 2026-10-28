using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.Activities;
using CMS.ContactManagement;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.EmailLibrary;
using CMS.OnlineForms;
using CMS.Websites;
using CMS.Websites.Internal;

namespace DancingGoat.Helpers.Generator
{
    /// <summary>
    /// Provides functionality to generate sample online marketing activities for a set of contacts, simulating user
    /// journeys such as page visits, form submissions, email clicks, and member registrations.
    /// </summary>
    /// <remarks>
    /// The generated activities are based on predefined journey templates and are intended for use
    /// in demonstration or testing scenarios. This class relies on various provider interfaces to retrieve and persist
    /// information about contacts, activities, web pages, forms, emails, and channels. Activities are generated only
    /// for contacts matching specific criteria and are distributed across different steps in each journey template.
    /// </remarks>
    public class ActivitiesGenerator
    {
        private const int DEFAULT_MAX_DAYS_AGO = 30;

        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IInfoProvider<ActivityInfo> activityInfoProvider;
        private readonly IInfoProvider<WebPageItemInfo> webPageItemInfoProvider;
        private readonly IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider;
        private readonly IInfoProvider<ChannelInfo> channelInfoProvider;
        private readonly IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider;
        private readonly IInfoProvider<ContactInfo> contactInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;
        private readonly IInfoProvider<EmailConfigurationInfo> emailConfigurationInfoProvider;
        private readonly IInfoProvider<EmailChannelInfo> emailChannelInfoProvider;

        private static readonly List<ActivityJourneyTemplate> ActivityJourneyTemplates =
        [
            new("Sample Program Sign-up Journey",
            [
                new(PredefinedActivityType.PAGE_VISIT, 100, new PageVisitActivityConfig(DigitalMarketingGeneratorConstants.COFFEE_SAMPLES_PAGE_GUID, "Coffee Samples")),
                new(PredefinedActivityType.BIZFORM_SUBMIT, 15, new FormSubmitActivityConfig(DigitalMarketingGeneratorConstants.COFFEE_SAMPLES_FORM_NAME, DigitalMarketingGeneratorConstants.COFFEE_SAMPLES_PAGE_GUID))
            ]),
            new("Newsletter Subscriber Journey",
            [
                new(PredefinedActivityType.PAGE_VISIT, 100, new PageVisitActivityConfig(DigitalMarketingGeneratorConstants.HOME_PAGE_GUID, "Home")),
                new(PredefinedActivityType.BIZFORM_SUBMIT, 80, new FormSubmitActivityConfig(DigitalMarketingGeneratorConstants.NEWSLETTER_SUBSCRIPTION_FORM_NAME, DigitalMarketingGeneratorConstants.HOME_PAGE_GUID)),
                new(PredefinedActivityType.EMAIL_CLICK, 15, new EmailClickActivityConfig(DigitalMarketingGeneratorConstants.NEWSLETTER_EMAIL_GUID, "Dancing Goat Regular (Email Builder)", "/articles")),
                new(PredefinedActivityType.MEMBER_REGISTRATION, 100, new MemberRegistrationActivityConfig()),
                new(PredefinedActivityType.PAGE_VISIT, 30, new PageVisitActivityConfig(DigitalMarketingGeneratorConstants.SECURED_PAGE_GUID, "Coffee Beverages Explained"))
            ])
        ];


        /// <summary>
        /// Initializes a new instance of the <see cref="ActivitiesGenerator"/> class.
        /// </summary>
        public ActivitiesGenerator(
            IWebPageUrlRetriever webPageUrlRetriever,
            IInfoProvider<ActivityInfo> activityInfoProvider,
            IInfoProvider<WebPageItemInfo> webPageItemInfoProvider,
            IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider,
            IInfoProvider<ChannelInfo> channelInfoProvider,
            IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
            IInfoProvider<ContactInfo> contactInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider,
            IInfoProvider<EmailConfigurationInfo> emailConfigurationInfoProvider,
            IInfoProvider<EmailChannelInfo> emailChannelInfoProvider)
        {
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.activityInfoProvider = activityInfoProvider;
            this.webPageItemInfoProvider = webPageItemInfoProvider;
            this.websiteChannelInfoProvider = websiteChannelInfoProvider;
            this.channelInfoProvider = channelInfoProvider;
            this.contentLanguageInfoProvider = contentLanguageInfoProvider;
            this.contactInfoProvider = contactInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
            this.emailConfigurationInfoProvider = emailConfigurationInfoProvider;
            this.emailChannelInfoProvider = emailChannelInfoProvider;
        }


        /// <summary>
        /// Generates and inserts sample activity data for contacts with a specific email domain.
        /// </summary>
        public async Task Generate()
        {
            var contacts = (await contactInfoProvider.Get()
                .WhereContains(nameof(ContactInfo.ContactEmail), DigitalMarketingGeneratorConstants.SAMPLE_CONTACTS_EMAIL_DOMAIN)
                .GetEnumerableTypedResultAsync())
                .ToList();

            if (contacts.Count == 0)
            {
                return;
            }

            var language = await contentLanguageInfoProvider.GetAsync(DigitalMarketingGeneratorConstants.DEFAULT_LANGUAGE_NAME);

            if (language == null)
            {
                return;
            }

            var allActivities = new List<ActivityInfo>();

            foreach (var journeyTemplate in ActivityJourneyTemplates)
            {
                var journeyActivities = await GenerateActivitiesForJourney(journeyTemplate, contacts, language.ContentLanguageID);
                allActivities.AddRange(journeyActivities);
            }

            if (allActivities.Count != 0)
            {
                activityInfoProvider.BulkInsert(allActivities);
            }
        }


        private async Task<List<ActivityInfo>> GenerateActivitiesForJourney(ActivityJourneyTemplate journeyTemplate, List<ContactInfo> contacts, int languageId)
        {
            // nosemgrep: dotnet.cryptography.CRYPTO-using-math-random - approved, not for crypto purposes.
            var random = new Random();
            var activities = new List<ActivityInfo>();
            var contactsForCurrentStep = contacts.ToList();

            foreach (var step in journeyTemplate.Steps)
            {
                var stepContactCount = (int)Math.Ceiling(contactsForCurrentStep.Count * step.Percentage / 100.0);
                var contactsForThisStep = contactsForCurrentStep.Take(stepContactCount).ToList();

                var contactContexts = new List<ActivityContext>();
                foreach (var contact in contactsForThisStep)
                {
                    var previousActivityDaysAgo = activities
                        .Where(a => a.ActivityContactID == contact.ContactID)
                        .OrderByDescending(a => a.ActivityCreated)
                        .Select(a => (DateTime.Now - a.ActivityCreated).Days)
                        .FirstOrDefault();

                    var maxDaysAgo = previousActivityDaysAgo > 0 ? previousActivityDaysAgo : DEFAULT_MAX_DAYS_AGO;
                    var daysAgo = random.Next(1, maxDaysAgo + 1);

                    contactContexts.Add(new ActivityContext(contact, daysAgo, languageId));
                }

                var stepActivities = await CreateActivitiesForStep(step, contactContexts);
                activities.AddRange(stepActivities);

                contactsForCurrentStep = contactsForThisStep;
            }

            return activities;
        }


        private async Task<List<ActivityInfo>> CreateActivitiesForStep(ActivityStepTemplate step, List<ActivityContext> contexts)
        {
            return step.ActivityType switch
            {
                PredefinedActivityType.PAGE_VISIT => await CreatePageVisitActivitiesFromConfig(step.Config as PageVisitActivityConfig, contexts),
                PredefinedActivityType.BIZFORM_SUBMIT => await CreateFormSubmitActivitiesFromConfig(step.Config as FormSubmitActivityConfig, contexts),
                PredefinedActivityType.EMAIL_CLICK => await CreateEmailClickActivitiesFromConfig(step.Config as EmailClickActivityConfig, contexts),
                PredefinedActivityType.MEMBER_REGISTRATION => CreateMemberRegistrationActivitiesFromConfig(contexts),
                _ => []
            };
        }


        private async Task<List<ActivityInfo>> CreatePageVisitActivitiesFromConfig(PageVisitActivityConfig config, List<ActivityContext> contexts)
        {
            var webPageItemGuid = Guid.Parse(config.WebPageGuid);
            var webPage = await webPageItemInfoProvider.GetAsync(webPageItemGuid);

            if (webPage == null)
            {
                return [];
            }

            var websiteChannel = await websiteChannelInfoProvider.GetAsync(webPage.WebPageItemWebsiteChannelID);
            var channel = await channelInfoProvider.GetAsync(websiteChannel.WebsiteChannelChannelID);

            if (channel == null)
            {
                return [];
            }

            var webPageUrl = await webPageUrlRetriever.Retrieve(webPageItemGuid, DigitalMarketingGeneratorConstants.DEFAULT_LANGUAGE_NAME);

            return [.. contexts.Select(context => CreatePageVisitActivity(
                context.Contact.ContactID,
                config.PageTitle,
                webPageItemGuid,
                webPageUrl.AbsoluteUrl,
                context.DaysAgo,
                context.LanguageId,
                channel.ChannelID))];
        }


        private async Task<List<ActivityInfo>> CreateFormSubmitActivitiesFromConfig(FormSubmitActivityConfig config, List<ActivityContext> contexts)
        {
            var form = await bizFormInfoProvider.GetAsync(config.FormName);

            if (form == null)
            {
                return [];
            }

            var webPageItemGuid = Guid.Parse(config.PageGuid);
            var webPage = await webPageItemInfoProvider.GetAsync(webPageItemGuid);

            if (webPage == null)
            {
                return [];
            }

            var websiteChannel = await websiteChannelInfoProvider.GetAsync(webPage.WebPageItemWebsiteChannelID);
            var channel = await channelInfoProvider.GetAsync(websiteChannel.WebsiteChannelChannelID);

            if (channel == null)
            {
                return [];
            }

            var webPageUrl = await webPageUrlRetriever.Retrieve(webPageItemGuid, DigitalMarketingGeneratorConstants.DEFAULT_LANGUAGE_NAME);

            return [.. contexts.Select(context => CreateFormSubmitActivity(
                context.Contact.ContactID,
                form.FormID,
                form.FormDisplayName,
                webPageUrl.AbsoluteUrl,
                context.DaysAgo,
                channel.ChannelID))];
        }


        private async Task<List<ActivityInfo>> CreateEmailClickActivitiesFromConfig(EmailClickActivityConfig config, List<ActivityContext> contexts)
        {
            var emailGuid = Guid.Parse(config.EmailGuid);
            var email = await emailConfigurationInfoProvider.GetAsync(emailGuid);

            if (email == null)
            {
                return [];
            }

            var emailChannel = await emailChannelInfoProvider.GetAsync(email.EmailConfigurationEmailChannelID);

            if (emailChannel == null)
            {
                return [];
            }

            return [.. contexts.Select(context => CreateEmailUrlClickActivity(
                context.Contact.ContactID,
                email.EmailConfigurationID,
                config.EmailTitle,
                config.ClickedUrl,
                context.DaysAgo,
                context.LanguageId,
                emailChannel.EmailChannelChannelID))];
        }


        private static List<ActivityInfo> CreateMemberRegistrationActivitiesFromConfig(List<ActivityContext> contexts)
        {
            return [.. contexts.Select(context => CreateMemberRegistrationActivity(
                context.Contact.ContactID,
                context.DaysAgo))];
        }


        private static ActivityInfo CreatePageVisitActivity(
            int contactId,
            string pageTitle,
            Guid webPageItemGuid,
            string webPageUrl,
            int daysAgo,
            int languageId,
            int channelId)
        {
            return new ActivityInfo
            {
                ActivityContactID = contactId,
                ActivityType = PredefinedActivityType.PAGE_VISIT,
                ActivityTitle = $"Page visit '{pageTitle}'",
                ActivityWebPageItemGUID = webPageItemGuid,
                ActivityURL = webPageUrl,
                ActivityCreated = DateTime.Now.AddDays(-daysAgo),
                ActivityLanguageID = languageId,
                ActivityChannelID = channelId
            };
        }


        private static ActivityInfo CreateFormSubmitActivity(
            int contactId,
            int formId,
            string formDisplayName,
            string webPageUrl,
            int daysAgo,
            int channelId)
        {
            return new ActivityInfo
            {
                ActivityContactID = contactId,
                ActivityType = PredefinedActivityType.BIZFORM_SUBMIT,
                ActivityItemID = formId,
                ActivityTitle = $"Form submitted '{formDisplayName}'",
                ActivityURL = webPageUrl,
                ActivityCreated = DateTime.Now.AddDays(-daysAgo),
                ActivityChannelID = channelId
            };
        }


        private static ActivityInfo CreateEmailUrlClickActivity(
            int contactId,
            int emailId,
            string emailTitle,
            string clickedUrl,
            int daysAgo,
            int languageId,
            int channelId)
        {
            return new ActivityInfo
            {
                ActivityContactID = contactId,
                ActivityType = PredefinedActivityType.EMAIL_CLICK,
                ActivityItemID = emailId,
                ActivityTitle = $"Clicked link in email '{emailTitle}'",
                ActivityValue = clickedUrl,
                ActivityCreated = DateTime.Now.AddDays(-daysAgo),
                ActivityLanguageID = languageId,
                ActivityChannelID = channelId
            };
        }


        private static ActivityInfo CreateMemberRegistrationActivity(
            int contactId,
            int daysAgo)
        {
            return new ActivityInfo
            {
                ActivityContactID = contactId,
                ActivityType = PredefinedActivityType.MEMBER_REGISTRATION,
                ActivityTitle = "Member registration",
                ActivityCreated = DateTime.Now.AddDays(-daysAgo),
            };
        }


        private record ActivityContext(ContactInfo Contact, int DaysAgo, int LanguageId);

        private record ActivityStepTemplate(string ActivityType, int Percentage, IActivityConfig Config);

        private record ActivityJourneyTemplate(string Name, ActivityStepTemplate[] Steps);

        private interface IActivityConfig { }

        private record PageVisitActivityConfig(string WebPageGuid, string PageTitle) : IActivityConfig;

        private record FormSubmitActivityConfig(string FormName, string PageGuid) : IActivityConfig;

        private record EmailClickActivityConfig(string EmailGuid, string EmailTitle, string ClickedUrl) : IActivityConfig;

        private record MemberRegistrationActivityConfig : IActivityConfig;
    }
}
