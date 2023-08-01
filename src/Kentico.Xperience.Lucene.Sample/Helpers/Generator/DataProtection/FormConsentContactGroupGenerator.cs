using CMS.ContactManagement;
using CMS.MacroEngine;
using CMS.Membership;

namespace DancingGoat.Helpers.Generator
{
    public class FormContactGroupGenerator
    {
        private const string CONTACT_GROUP_DISPLAY_NAME = "Coffee samples applicants";
        private const string CONTACT_GROUP_NAME = "CoffeeSamplesApplicants";


        public void Generate()
        {
            CreateContactGroupWithFormConsentAgreementRule();
        }


        private void CreateContactGroupWithFormConsentAgreementRule()
        {
            if (ContactGroupInfo.Provider.Get(CONTACT_GROUP_NAME) != null)
            {
                return;
            }

            var contactGroup = new ContactGroupInfo
            {
                ContactGroupDisplayName = CONTACT_GROUP_DISPLAY_NAME,
                ContactGroupName = CONTACT_GROUP_NAME,
                ContactGroupDynamicCondition = GetFormConsentMacroRule(),
                ContactGroupEnabled = true
            };

            ContactGroupInfo.Provider.Set(contactGroup);
        }


        private string GetFormConsentMacroRule()
        {
            var rule = $@"{{%Rule(""(Contact.AgreedWithConsent(""{FormConsentGenerator.CONSENT_NAME}""))"", "" <rules><r pos =\""0\"" par=\""\"" op=\""and\"" n=\""CMSContactHasAgreedWithConsent\"" >
                        <p n=\""consent\""><t>{FormConsentGenerator.CONSENT_DISPLAY_NAME}</t><v>{FormConsentGenerator.CONSENT_NAME}</v><r>0</r><d>select consent</d><vt>text</vt><tv>0</tv></p>
                        <p n=\""_perfectum\""><t>has</t><v></v><r>0</r><d>select operation</d><vt>text</vt><tv>0</tv></p></r></rules>"")%}}";

            return MacroSecurityProcessor.AddSecurityParameters(rule, MacroIdentityOption.FromUserInfo(UserInfoProvider.AdministratorUser), null);
        }
    }
}