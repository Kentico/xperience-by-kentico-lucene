using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CMS.CustomerJourneys.Internal;
using CMS.MacroEngine;
using CMS.Membership;
using CMS.Scheduler.Internal;

namespace DancingGoat.Helpers.Generator
{
    /// <summary>
    /// Contains methods for generating sample customer journeys for online marketing demonstrations.
    /// </summary>
    public class CustomerJourneyGenerator
    {
        private readonly ICustomerJourneyManager customerJourneyManager;
        private readonly ICustomerJourneyScheduleTaskManager customerJourneyScheduleTaskManager;
        private readonly ISchedulingExecutor schedulingExecutor;

        private static readonly List<JourneyTemplate> JourneyTemplates =
        [
            new("Newsletter Engagement to Membership",
            [
                new("Discovery - Homepage Visit", 200, GetPageVisitMacroRule(Guid.Parse(DigitalMarketingGeneratorConstants.HOME_PAGE_GUID))),
                new("Interest - Newsletter Signup", 50, GetFormSubmissionMacroRule(DigitalMarketingGeneratorConstants.NEWSLETTER_SUBSCRIPTION_FORM_NAME)),
                new("Engagement - Email Link Click", 30, GetClickOnLinkInEmailMacroRule(Guid.Parse(DigitalMarketingGeneratorConstants.NEWSLETTER_EMAIL_GUID))),
                new("Conversion - Member Registration", 25, GetHasBecomeMemberMacroRule()),
                new("Loyalty - Member Content Access", 20, GetPageVisitMacroRule(Guid.Parse(DigitalMarketingGeneratorConstants.SECURED_PAGE_GUID)))
            ]),
            new("Coffee Sample Program Acquisition",
            [
                new("Awareness - Coffee Samples Page", 200, GetPageVisitMacroRule(Guid.Parse(DigitalMarketingGeneratorConstants.COFFEE_SAMPLES_PAGE_GUID))),
                new("Conversion - Sample Request", 50, GetFormSubmissionMacroRule(DigitalMarketingGeneratorConstants.COFFEE_SAMPLES_FORM_NAME))
            ])
        ];


        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerJourneyGenerator"/> class.
        /// </summary>
        public CustomerJourneyGenerator(ICustomerJourneyManager customerJourneyManager, ICustomerJourneyScheduleTaskManager customerJourneyScheduleTaskManager, ISchedulingExecutor schedulingExecutor)
        {
            this.customerJourneyManager = customerJourneyManager;
            this.customerJourneyScheduleTaskManager = customerJourneyScheduleTaskManager;
            this.schedulingExecutor = schedulingExecutor;
        }


        /// <summary>
        /// Generates sample customer journeys. Suitable only for Dancing Goat demo site.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task Generate(CancellationToken cancellationToken = default)
        {
            var customerJourneys = await CreateSampleCustomerJourneys(cancellationToken);

            foreach (var customerJourneyInfo in customerJourneys)
            {
                var recalculationTask = await customerJourneyScheduleTaskManager.CreateNewRecalculationTask(customerJourneyInfo, null, cancellationToken);
                await schedulingExecutor.ExecuteTask(recalculationTask.ScheduledTaskConfigurationID, cancellationToken);
            }
        }


        private async Task<IEnumerable<CustomerJourneyInfo>> CreateSampleCustomerJourneys(CancellationToken cancellationToken)
        {
            var journeys = new List<CustomerJourneyInfo>();

            foreach (var template in JourneyTemplates)
            {
                var journey = await customerJourneyManager.Create(cancellationToken: cancellationToken);

                journey.CustomerJourneyDisplayName = template.DisplayName;
                journey.Update();

                journeys.Add(journey);

                CreateStagesForJourney(journey, template);
            }

            return journeys;
        }


        private static void CreateStagesForJourney(CustomerJourneyInfo journey, JourneyTemplate template)
        {
            for (int order = 0; order < template.Stages.Length; order++)
            {
                var stageTemplate = template.Stages[order];
                var stage = new CustomerJourneyStageInfo
                {
                    CustomerJourneyStageCustomerJourneyID = journey.CustomerJourneyID,
                    CustomerJourneyStageName = GenerateCodeName(stageTemplate.DisplayName),
                    CustomerJourneyStageDisplayName = stageTemplate.DisplayName,
                    CustomerJourneyStageOrder = order,
                    CustomerJourneyStageKPI = stageTemplate.Kpi,
                    CustomerJourneyStageDynamicCondition = stageTemplate.DynamicCondition ?? string.Empty
                };
                stage.Insert();
            }
        }


        private static string GenerateCodeName(string displayName)
        {
            return displayName
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);
        }


        private static string GetPageVisitMacroRule(Guid webPageGuid)
        {
            var rule = $@"{{%Rule(""(Contact.VisitedPage(\""{webPageGuid}\""))"", ""<rules><r pos=\""0\"" par=\""\"" op=\""and\"" /><r pos=\""0\"" par=\""0\"" op=\""and\"" n=\""CMSContactHasVisitedPage\"" ><p n=\""page\""><t>#select page</t><v>{webPageGuid}</v><r>1</r><d>select page</d><vt>pages</vt><tv>0</tv></p></r></rules>"")%}}";

            return AddSecurityParameters(rule);
        }


        private static string GetFormSubmissionMacroRule(string formName)
        {
            var rule = $@"{{%Rule(""(Contact.SubmittedForm(\""{formName}\""))"", ""<rules><r pos=\""0\"" par=\""\"" op=\""and\"" /><r pos=\""0\"" par=\""0\"" op=\""and\"" n=\""CMSContactHasSubmittedForm\"" ><p n=\""form\""><t>#form</t><v>{formName}</v><r>1</r><d>form</d><vt>objectcodenames</vt><tv>0</tv></p></r></rules>"")%}}";

            return AddSecurityParameters(rule);
        }


        private static string GetClickOnLinkInEmailMacroRule(Guid emailConfigurationGuid)
        {
            var rule = $@"{{%Rule(""(Contact.ClickedOnSpecificLinkInEmail(\""{emailConfigurationGuid}\"", \""Contains\"", \""/articles\""))"", ""<rules><r pos=\""0\"" par=\""\"" op=\""and\"" /><r pos=\""0\"" par=\""0\"" op=\""and\"" n=\""CMSContactHasClickedOnSpecificEmailUrl\"" ><p n=\""value\""><t>#enter value</t><v>/articles</v><r>1</r><d>enter value</d><vt>text</vt><tv>0</tv></p><p n=\""op\""><t>contains</t><v>Contains</v><r>1</r><d>select operator</d><vt>text</vt><tv>0</tv></p><p n=\""email\""><t>#select email</t><v>{emailConfigurationGuid}</v><r>1</r><d>select email</d><vt>emails</vt><tv>0</tv></p></r></rules>"")%}}";

            return AddSecurityParameters(rule);
        }


        private static string GetHasBecomeMemberMacroRule()
        {
            var rule = $@"{{%Rule(""(Contact.HasBecomeMember())"", ""<rules><r pos=\""0\"" par=\""\"" op=\""and\"" /><r pos=\""0\"" par=\""0\"" op=\""and\"" n=\""CMSContactHasBecomeMember\"" ></r></rules>"")%}}";

            return AddSecurityParameters(rule);
        }


        private static string AddSecurityParameters(string rule)
        {
            return MacroSecurityProcessor.AddSecurityParameters(rule, MacroIdentityOption.FromUserInfo(UserInfoProvider.AdministratorUser), null);
        }


        private record StageTemplate(string DisplayName, int Kpi = 50, string DynamicCondition = null);

        private record JourneyTemplate(string DisplayName, StageTemplate[] Stages);
    }
}
