using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CMS.Base;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Membership;
using CMS.OnlineForms;
using CMS.Websites;

using DancingGoat.AdminComponents;
using DancingGoat.Helpers.Generator;

using Kentico.Forms.Web.Mvc.Internal;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

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
        private readonly IEventLogService eventLogService;
        private readonly IInfoProvider<ConsentInfo> consentInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;
        private readonly IInfoProvider<ContactGroupInfo> contactGroupInfoProvider;
        private readonly IInfoProvider<SettingsKeyInfo> settingsKeyInfoProvider;
        private readonly IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleDataGeneratorApplication"/> class.
        /// </summary>
        /// <param name="formBuilderConfigurationSerializer">Form builder configuration serializer.</param>
        /// <param name="eventLogService">Event log service.</param>
        /// <param name="consentInfoProvider">Consent info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provider.</param>
        /// <param name="contactGroupInfoProvider">Contact group info provider.</param>
        /// <param name="settingsKeyInfoProvider">Settings key info provider.</param>
        /// <param name="websiteChannelInfoProvider">Website channel info provider.</param>
        public SampleDataGeneratorApplication(
            IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer,
            IEventLogService eventLogService,
            IInfoProvider<ConsentInfo> consentInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider,
            IInfoProvider<ContactGroupInfo> contactGroupInfoProvider,
            IInfoProvider<SettingsKeyInfo> settingsKeyInfoProvider,
            IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider)
        {
            this.formBuilderConfigurationSerializer = formBuilderConfigurationSerializer;
            this.eventLogService = eventLogService;
            this.consentInfoProvider = consentInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
            this.contactGroupInfoProvider = contactGroupInfoProvider;
            this.settingsKeyInfoProvider = settingsKeyInfoProvider;
            this.websiteChannelInfoProvider = websiteChannelInfoProvider;
        }


        public override Task ConfigurePage()
        {
            PageConfiguration.CardGroups.AddCardGroup().AddCard(GetGdprCard());

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
                new FormContactGroupGenerator(contactGroupInfoProvider).Generate();

                EnableDataProtectionSamples();

                await SetChannelDefaultCookieLevelToEssential(DANCING_GOAT_WEBSITE_CHANNEL_ID);
            }
            catch (Exception ex)
            {
                eventLogService.LogException("SampleDataGenerator", "GDPR", ex);

                return Response().AddErrorMessage("GDPR sample data generator failed. See event log for more details.");
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


        private OverviewCard GetGdprCard()
        {
            return new OverviewCard
            {
                Headline = "Set up data protection (GDPR) demo",
                Actions = new[]
                {
                    new Kentico.Xperience.Admin.Base.Action(ActionType.Command)
                    {
                        Label = "Generate",
                        Parameter = nameof(GenerateGdprSampleData),
                        ButtonColor = ButtonColor.Secondary
                    }
                },
                Components = new List<IOverviewCardComponent>()
                {
                    new StringContentCardComponent
                    {
                        Content =  @"Generates data and enables demonstration of giving consents, personal data portability, right to access, and right to be forgotten features.
                            Once enabled, the demo functionality cannot be disabled. Use on demo instances only."
                    }
                }
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
