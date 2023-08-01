using System;
using System.Collections.Generic;

using CMS;
using CMS.Base;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;

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


        /// <summary>
        /// Initializes a new instance of the <see cref="DancingGoatSamplesModule"/> class.
        /// </summary>
        public DancingGoatSamplesModule()
            : base("DancingGoatSamplesModule")
        {
        }


        /// <summary>
        /// Initializes the module.
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            InitializeSamples();
        }


        /// <summary>
        /// Registers sample personal data collectors immediately or attaches an event handler to register the collectors upon dedicated key insertion.
        /// Disabling or toggling registration of the sample collectors is not supported.
        /// </summary>
        private void InitializeSamples()
        {
            var dataProtectionSamplesEnabledSettingsKey = SettingsKeyInfoProvider.GetSettingsKeyInfo(DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME);
            if (dataProtectionSamplesEnabledSettingsKey?.KeyValue.ToBoolean(false) ?? false)
            {
                RegisterSamples();
            }
            else
            {
                SettingsKeyInfoProvider.OnSettingsKeyChanged += (sender, eventArgs) =>
                {
                    if (eventArgs.KeyName.Equals(DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME, StringComparison.OrdinalIgnoreCase) &&
                        (eventArgs.Action == SettingsKeyActionEnum.Insert) && eventArgs.KeyValue.ToBoolean(false))
                    {
                        RegisterSamples();
                    }
                };
            }
        }


        internal static void RegisterSamples()
        {
            IdentityCollectorRegister.Instance.Add(new SampleContactInfoIdentityCollector());
            IdentityCollectorRegister.Instance.Add(new SampleMemberInfoIdentityCollector());

            PersonalDataCollectorRegister.Instance.Add(new SampleContactDataCollector());
            PersonalDataCollectorRegister.Instance.Add(new SampleMemberDataCollector());

            PersonalDataEraserRegister.Instance.Add(new SampleContactPersonalDataEraser());
            PersonalDataEraserRegister.Instance.Add(new SampleMemberPersonalDataEraser());

            RegisterConsentRevokeHandler();
        }


        internal static void DeleteContactActivities(ContactInfo contact)
        {
            var configuration = new Dictionary<string, object>
            {
                { "deleteActivities", true }
            };

            new SampleContactPersonalDataEraser().Erase(new[] { contact }, configuration);
        }


        private static void RegisterConsentRevokeHandler()
        {
            DataProtectionEvents.RevokeConsentAgreement.Execute += (sender, args) =>
            {
                if (args.Consent.ConsentName.Equals("DancingGoatTracking", StringComparison.Ordinal))
                {
                    DeleteContactActivities(args.Contact);

                    // Remove cookies used for contact tracking
                    CookieHelper.Remove(CookieName.CurrentContact);
                    CookieHelper.Remove(CookieName.CrossSiteContact);

                    // Set the cookie level to default
                    var cookieLevelProvider = Service.Resolve<ICurrentCookieLevelProvider>();
                    cookieLevelProvider.SetCurrentCookieLevel(cookieLevelProvider.GetDefaultCookieLevel());
                }
            };
        }
    }
}
