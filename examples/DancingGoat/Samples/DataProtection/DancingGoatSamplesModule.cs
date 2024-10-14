using System;
using System.Collections.Generic;

using CMS;
using CMS.Activities;
using CMS.Base;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Globalization;
using CMS.Helpers;
using CMS.Membership;
using CMS.OnlineForms;

using DancingGoat.Helpers.Generator;

using Kentico.Web.Mvc;

using Samples.DancingGoat;

[assembly: RegisterModule(typeof(DancingGoatSamplesModule))]

namespace Samples.DancingGoat
{
    /// <summary>
    /// Represents module with DataProtection sample code.
    /// </summary>
    internal class DancingGoatSamplesModule : Module
    {
        private const string DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME = "DataProtectionSamplesEnabled";

        private IInfoProvider<ContactInfo> contactInfoProvider;
        private IMemberInfoProvider memberInfoProvider;
        private IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider;
        private IInfoProvider<BizFormInfo> bizFormInfoProvider;
        private IInfoProvider<AccountContactInfo> accountContactInfoProvider;
        private IInfoProvider<SettingsKeyInfo> settingsKeyInfoProvider;
        private IInfoProvider<ActivityInfo> activityInfoProvider;
        private IInfoProvider<CountryInfo> countryInfoProvider;
        private IInfoProvider<StateInfo> stateInfoProvider;
        private IInfoProvider<AccountInfo> accountInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="DancingGoatSamplesModule"/> class.
        /// </summary>
        public DancingGoatSamplesModule() : base(nameof(DancingGoatSamplesModule))
        {
        }


        /// <summary>
        /// Initializes the module.
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            contactInfoProvider = Service.Resolve<IInfoProvider<ContactInfo>>();
            memberInfoProvider = Service.Resolve<IMemberInfoProvider>();
            consentAgreementInfoProvider = Service.Resolve<IInfoProvider<ConsentAgreementInfo>>();
            bizFormInfoProvider = Service.Resolve<IInfoProvider<BizFormInfo>>();
            accountContactInfoProvider = Service.Resolve<IInfoProvider<AccountContactInfo>>();
            settingsKeyInfoProvider = Service.Resolve<IInfoProvider<SettingsKeyInfo>>();
            activityInfoProvider = Service.Resolve<IInfoProvider<ActivityInfo>>();
            countryInfoProvider = Service.Resolve<IInfoProvider<CountryInfo>>();
            stateInfoProvider = Service.Resolve<IInfoProvider<StateInfo>>();
            accountInfoProvider = Service.Resolve<IInfoProvider<AccountInfo>>();

            InitializeSamples();
        }


        /// <summary>
        /// Registers sample personal data collectors immediately or attaches an event handler to register the collectors upon dedicated key insertion.
        /// Disabling or toggling registration of the sample collectors is not supported.
        /// </summary>
        private void InitializeSamples()
        {
            var dataProtectionSamplesEnabledSettingsKey = settingsKeyInfoProvider.Get(DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME);
            if (dataProtectionSamplesEnabledSettingsKey?.KeyValue.ToBoolean(false) ?? false)
            {
                RegisterSamples();
            }
            else
            {
                SettingsKeyInfo.TYPEINFO.Events.Insert.After += (sender, eventArgs) =>
                {
                    var settingKey = eventArgs.Object as SettingsKeyInfo;
                    if (settingKey.KeyName.Equals(DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME, StringComparison.OrdinalIgnoreCase)
                        && settingKey.KeyValue.ToBoolean(false))
                    {
                        RegisterSamples();
                    }
                };
            }
        }


        internal void RegisterSamples()
        {
            IdentityCollectorRegister.Instance.Add(new SampleContactInfoIdentityCollector(contactInfoProvider));
            IdentityCollectorRegister.Instance.Add(new SampleMemberInfoIdentityCollector(memberInfoProvider));

            PersonalDataCollectorRegister.Instance.Add(new SampleContactDataCollector(activityInfoProvider, countryInfoProvider, stateInfoProvider, consentAgreementInfoProvider,
                accountContactInfoProvider, accountInfoProvider, bizFormInfoProvider));
            PersonalDataCollectorRegister.Instance.Add(new SampleMemberDataCollector());

            PersonalDataEraserRegister.Instance.Add(new SampleContactPersonalDataEraser(consentAgreementInfoProvider, bizFormInfoProvider, accountContactInfoProvider, contactInfoProvider, activityInfoProvider));
            PersonalDataEraserRegister.Instance.Add(new SampleMemberPersonalDataEraser(memberInfoProvider));

            RegisterConsentRevokeHandler();
        }


        internal void DeleteContactActivities(ContactInfo contact)
        {
            var configuration = new Dictionary<string, object>
            {
                { "deleteActivities", true }
            };

            new SampleContactPersonalDataEraser(consentAgreementInfoProvider, bizFormInfoProvider, accountContactInfoProvider, contactInfoProvider, activityInfoProvider)
                    .Erase(new[] { contact }, configuration);
        }


        private void RegisterConsentRevokeHandler()
        {
            DataProtectionEvents.RevokeConsentAgreement.Execute += (sender, args) =>
            {
                if (args.Consent.ConsentName.Equals(TrackingConsentGenerator.CONSENT_NAME, StringComparison.Ordinal))
                {
                    DeleteContactActivities(args.Contact);

                    // Remove cookies used for contact tracking
                    var cookieAccessor = Service.Resolve<ICookieAccessor>();

#pragma warning disable CS0618 // CookieName is obsolete
                    cookieAccessor.Remove(CookieName.CurrentContact);
                    cookieAccessor.Remove(CookieName.CrossSiteContact);
#pragma warning restore CS0618 // CookieName is obsolete


                    // Set the cookie level to default
                    var cookieLevelProvider = Service.Resolve<ICurrentCookieLevelProvider>();
                    cookieLevelProvider.SetCurrentCookieLevel(cookieLevelProvider.GetDefaultCookieLevel());
                }
            };
        }
    }
}
