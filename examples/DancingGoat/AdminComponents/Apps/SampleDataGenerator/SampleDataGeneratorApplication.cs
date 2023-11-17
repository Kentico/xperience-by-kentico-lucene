using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailLibrary;
using CMS.Membership;

using Kentico.Forms.Web.Mvc.Internal;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

using DancingGoat.AdminComponents;
using DancingGoat.Helpers.Generator;

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


        private const string FORM_NAME = "DancingGoatCoreCoffeeSampleList";
        private const string FORM_FIELD_NAME = "Consent";
        private const string DATA_PROTECTION_SETTINGS_KEY = "DataProtectionSamplesEnabled";
        private readonly ISiteService siteService;
        private readonly IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer;
        private readonly IEventLogService eventLogService;


        public SampleDataGeneratorApplication(ISiteService siteService,
            IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer,
            IEventLogService eventLogService)
        {
            this.siteService = siteService;
            this.formBuilderConfigurationSerializer = formBuilderConfigurationSerializer;
            this.eventLogService = eventLogService;
        }


        public override Task ConfigurePage()
        {
            var gdprCard = new OverviewCard
            {
                Headline = "Set up data protection (GDPR) demo",
                Actions = new[]
                {
                    new Kentico.Xperience.Admin.Base.Action(ActionType.Command)
                    {
                        Label = "Generate",
                        Parameter = nameof(GenerateGdprSampleData)
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

            var emailStatisticsCard = new OverviewCard
            {
                Headline = "Email statistics sample data",
                Actions = new[]
                {
                    new Kentico.Xperience.Admin.Base.Action(ActionType.Command)
                    {
                        Label = "Generate sample data",
                        Parameter = nameof(GenerateEmailStatisticsSampleData)
                    },
                    new Kentico.Xperience.Admin.Base.Action(ActionType.Command)
                    {
                        Label = "Recalculate statistics",
                        Parameter = nameof(RecalculateStatistics)
                    }
                },
                Components = new List<IOverviewCardComponent>()
                {
                    new StringContentCardComponent
                    {
                        Content =  "Generates emails and a recipient list containing contacts, together with realistically looking email statistics. To immediately see the statistics in the Emails application, select the 'Generate sample data' button and then 'Recalculate statistics'."
                    }
                }
            };
            PageConfiguration.CardGroups.AddCardGroup().AddCard(gdprCard);
            PageConfiguration.CardGroups.AddCardGroup().AddCard(emailStatisticsCard);

            PageConfiguration.Caption = "Generator";

            return base.ConfigurePage();
        }


        [PageCommand(Permission = SystemPermissions.VIEW)]
        public Task<ICommandResponse> GenerateGdprSampleData()
        {
            try
            {
                var site = siteService.CurrentSite;

                new TrackingConsentGenerator(site).Generate();
                new FormConsentGenerator(site, formBuilderConfigurationSerializer).Generate(FORM_NAME, FORM_FIELD_NAME);
                new FormContactGroupGenerator().Generate();

                EnableDataProtectionSamples();
            }
            catch (Exception ex)
            {
                eventLogService.LogException("SampleDataGenerator", "GDPR", ex);

                return Task.FromResult(Response()
                    .AddErrorMessage("GDPR sample data generator failed. See event log for more details"));
            }

            return Task.FromResult(Response()
                    .AddSuccessMessage("Generating data finished successfully."));
        }
        
        
        [PageCommand(Permission = SystemPermissions.VIEW)]
        public async Task<ICommandResponse> GenerateEmailStatisticsSampleData()
        {
            try
            {
                await new EmailLibrarySampleGenerator(siteService.CurrentSite).GenerateDancingGoatMailSample();

                return Response()
                    .AddSuccessMessage("Email statistics sample data generated successfully.");
            }
            catch (Exception ex)
            {
                eventLogService.LogException("SampleDataGenerator", "EmailStatistics", ex);

                return Response()
                    .AddErrorMessage("Email statistics sample data generator failed. See event log for more details");
            }
        }


        [PageCommand(Permission = SystemPermissions.VIEW)]
        public async Task<ICommandResponse> RecalculateStatistics()
        {
            try
            {
                var configurations = EmailConfigurationInfo.Provider.Get()
                    .WhereContains(nameof(EmailConfigurationInfo.EmailConfigurationName), EmailLibrarySampleGenerator.GeneratedInfoCodeNameSuffix)
                    .ToArray();

                foreach (var configuration in configurations)
                {
                    await ConnectionHelper.ExecuteNonQueryAsync(
                        $"{EmailStatisticsInfo.TYPEINFO.ObjectClassName}.RecalculateEmailStatistics",
                        CancellationToken.None,
                        new QueryDataParameters { new("@EmailConfigurationID", configuration.EmailConfigurationID) });
                }

                ProviderHelper.ClearHashtables(EmailStatisticsInfo.OBJECT_TYPE, true);

                return Response()
                    .AddSuccessMessage("Email statistics have been recalculated successfully.");
            }
            catch (Exception ex)
            {
                eventLogService.LogException("SampleDataGenerator", "EmailStatisticsRecalculation", ex);

                return Response()
                    .AddErrorMessage("Email statistics recalculation failed. See event log for more details");
            }
        }


        private void EnableDataProtectionSamples()
        {
            var dataProtectionSamplesEnabledSettingsKey = SettingsKeyInfoProvider.GetSettingsKeyInfo(DATA_PROTECTION_SETTINGS_KEY);
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
                KeyDefaultValue = "False",
                KeyIsGlobal = true,
                KeyIsHidden = true
            };

            SettingsKeyInfoProvider.SetSettingsKeyInfo(keyInfo);
        }
    }
}
