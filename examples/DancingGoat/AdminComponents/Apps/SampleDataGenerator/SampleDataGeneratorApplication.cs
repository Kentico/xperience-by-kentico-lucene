using System;
using System.Threading.Tasks;

using CMS.Activities;
using CMS.Base;
using CMS.ContactManagement;
using CMS.ContentEngine;
using CMS.CustomerJourneys.Internal;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailLibrary;
using CMS.Membership;
using CMS.OnlineForms;
using CMS.Scheduler.Internal;
using CMS.Websites;
using CMS.Websites.Internal;

using DancingGoat.AdminComponents;
using DancingGoat.Helpers.Generator;

using Kentico.Forms.Web.Mvc.Internal;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

using Microsoft.Extensions.Logging;

[assembly: UIApplication(SampleDataGeneratorApplication.IDENTIFIER, typeof(SampleDataGeneratorApplication), "sample-data-generator", "Sample data generator", BaseApplicationCategories.CONFIGURATION, Icons.CogwheelSquare, TemplateNames.OVERVIEW)]

namespace DancingGoat.AdminComponents
{
    /// <summary>
    /// Represents an application for sample data generation.
    /// </summary>
    [UIPermission(SystemPermissions.VIEW)]
    public class SampleDataGeneratorApplication : OverviewPageBase
    {
        /// <summary>
        /// Unique identifier of application.
        /// </summary>
        public const string IDENTIFIER = "Kentico.Xperience.Application.SampleDataGenerator";

        private const int DANCING_GOAT_WEBSITE_CHANNEL_ID = 1;
        private const string FORM_NAME = "DancingGoatCoffeeSampleList";
        private const string FORM_FIELD_NAME = "Consent";
        private const string DATA_PROTECTION_SETTINGS_KEY = "DataProtectionSamplesEnabled";

        private readonly IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer;
        private readonly ILogger<SampleDataGeneratorApplication> logger;
        private readonly IInfoProvider<ConsentInfo> consentInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;
        private readonly IInfoProvider<ContactGroupInfo> contactGroupInfoProvider;
        private readonly IInfoProvider<SettingsKeyInfo> settingsKeyInfoProvider;
        private readonly IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider;
        private readonly ContactsGenerator contactsGenerator;
        private readonly ActivitiesGenerator activitiesGenerator;
        private readonly CustomerJourneyGenerator customerJourneyGenerator;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleDataGeneratorApplication"/> class.
        /// </summary>
        public SampleDataGeneratorApplication(
            IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer,
            ICustomerJourneyManager customerJourneyManager,
            ICustomerJourneyScheduleTaskManager customerJourneyScheduleTaskManager,
            ISchedulingExecutor schedulingExecutor,
            ILogger<SampleDataGeneratorApplication> logger,
            IWebPageUrlRetriever webPageUrlRetriever,
            IInfoProvider<ConsentInfo> consentInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider,
            IInfoProvider<ContactGroupInfo> contactGroupInfoProvider,
            IInfoProvider<SettingsKeyInfo> settingsKeyInfoProvider,
            IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider,
            IInfoProvider<ContactInfo> contactInfoProvider,
            IInfoProvider<ActivityInfo> activityInfoProvider,
            IInfoProvider<WebPageItemInfo> webPageItemInfoProvider,
            IInfoProvider<ChannelInfo> channelInfoProvider,
            IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
            IInfoProvider<EmailConfigurationInfo> emailConfigurationInfoProvider,
            IInfoProvider<EmailChannelInfo> emailChannelInfoProvider)
        {
            this.formBuilderConfigurationSerializer = formBuilderConfigurationSerializer;
            this.logger = logger;
            this.consentInfoProvider = consentInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
            this.contactGroupInfoProvider = contactGroupInfoProvider;
            this.settingsKeyInfoProvider = settingsKeyInfoProvider;
            this.websiteChannelInfoProvider = websiteChannelInfoProvider;

            contactsGenerator = new ContactsGenerator(contactInfoProvider);
            activitiesGenerator = new ActivitiesGenerator(webPageUrlRetriever, activityInfoProvider, webPageItemInfoProvider, websiteChannelInfoProvider, channelInfoProvider, contentLanguageInfoProvider, contactInfoProvider, bizFormInfoProvider, emailConfigurationInfoProvider, emailChannelInfoProvider);
            customerJourneyGenerator = new CustomerJourneyGenerator(customerJourneyManager, customerJourneyScheduleTaskManager, schedulingExecutor);
        }


        public override Task ConfigurePage()
        {
            PageConfiguration.CardGroups.AddCardGroup().AddCard(CreateGeneratorCard(
                headline: "Set up data protection (GDPR) demo",
                commandParameter: nameof(GenerateGdprSampleData),
                content: @"Generates data and enables demonstration of giving consents, personal data portability, right to access, and right to be forgotten features.
                    Once enabled, the demo functionality cannot be disabled. Use on demo instances only."));

            PageConfiguration.CardGroups.AddCardGroup().AddCard(CreateGeneratorCard(
                headline: "Generate digital marketing sample data",
                commandParameter: nameof(GenerateDigitalMarketingData),
                content: @"To enable a demonstration of digital marketing features, the generator creates sample contacts, activity data and customer journeys.
                    The generator does not overwrite your custom data."));

            PageConfiguration.Caption = "Sample data generator";

            return base.ConfigurePage();
        }


        [PageCommand(Permission = SystemPermissions.VIEW)]
        public async Task<ICommandResponse> GenerateGdprSampleData()
        {
            try
            {
                new TrackingConsentGenerator(consentInfoProvider).Generate();
                new FormConsentGenerator(formBuilderConfigurationSerializer, consentInfoProvider, bizFormInfoProvider).Generate(FORM_NAME, FORM_FIELD_NAME);
                new FormConsentContactGroupGenerator(contactGroupInfoProvider).Generate();

                EnableDataProtectionSamples();

                await SetChannelDefaultCookieLevelToEssential(DANCING_GOAT_WEBSITE_CHANNEL_ID);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GDPR sample data generation failed.");

                return Response().AddErrorMessage("GDPR sample data generator failed. See event log for more details.");
            }

            return Response().AddSuccessMessage("Generating data finished successfully.");
        }


        [PageCommand(Permission = SystemPermissions.VIEW)]
        public async Task<ICommandResponse> GenerateDigitalMarketingData()
        {
            try
            {
                contactsGenerator.Generate();
                await activitiesGenerator.Generate();
                await customerJourneyGenerator.Generate();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Digital marketing sample data generation failed.");

                return Response().AddErrorMessage("Digital marketing sample data generator failed. See event log for more details.");
            }

            return Response().AddSuccessMessage("Generating data finished successfully.");
        }


        private void EnableDataProtectionSamples()
        {
            var dataProtectionSamplesEnabledSettingsKey = settingsKeyInfoProvider.Get(DATA_PROTECTION_SETTINGS_KEY);
            if (dataProtectionSamplesEnabledSettingsKey?.KeyValue.ToBoolean(false) ?? false)
            {
                return;
            }

            var keyInfo = new SettingsKeyInfo
            {
                KeyName = DATA_PROTECTION_SETTINGS_KEY,
                KeyDisplayName = DATA_PROTECTION_SETTINGS_KEY,
                KeyType = "boolean",
                KeyValue = "True",
                KeyIsHidden = true,
            };

            settingsKeyInfoProvider.Set(keyInfo);
        }


        private static OverviewCard CreateGeneratorCard(string headline, string commandParameter, string content)
        {
            return new OverviewCard
            {
                Headline = headline,
                Actions =
                [
                    new Kentico.Xperience.Admin.Base.Action(ActionType.Command)
                    {
                        Label = "Generate",
                        Parameter = commandParameter,
                        ButtonColor = ButtonColor.Secondary
                    }
                ],
                Components =
                [
                    new StringContentCardComponent
                    {
                        Content = content
                    }
                ]
            };
        }


        private async Task SetChannelDefaultCookieLevelToEssential(int websiteChannelId)
        {
            var websiteChannel = await websiteChannelInfoProvider.GetAsync(websiteChannelId);

            if (websiteChannel is not null)
            {
                websiteChannel.WebsiteChannelDefaultCookieLevel = Kentico.Web.Mvc.CookieLevel.Essential.Level;
                websiteChannel.Generalized.SetObject();
            }
        }
    }
}
