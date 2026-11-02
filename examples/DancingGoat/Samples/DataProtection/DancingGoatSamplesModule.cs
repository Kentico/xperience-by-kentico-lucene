using System;
using System.Collections.Generic;

using CMS;
using CMS.Activities;
using CMS.Base;
using CMS.Commerce;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Globalization;
using CMS.Helpers;
using CMS.Membership;
using CMS.Notifications;
using CMS.OnlineForms;

using DancingGoat.Helpers.Generator;

using Kentico.Web.Mvc;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Samples.DancingGoat;

[assembly: RegisterModule(typeof(DancingGoatSamplesModule))]

namespace Samples.DancingGoat
{
    /// <summary>
    /// Represents module with DancingGoat sample code (data protection collectors and the automation notification placeholder configuration).
    /// </summary>
    internal class DancingGoatSamplesModule : Module
    {
        private const string DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME = "DataProtectionSamplesEnabled";


        /// <summary>
        /// Initializes a new instance of the <see cref="DancingGoatSamplesModule"/> class.
        /// </summary>
        public DancingGoatSamplesModule() : base(nameof(DancingGoatSamplesModule))
        {
        }


        /// <summary>
        /// Registers the DancingGoat event handlers.
        /// </summary>
        protected override void OnPreInit(ModulePreInitParameters parameters)
        {
            base.OnPreInit(parameters);

            parameters.Services.AddInfoObjectEventHandler<InfoObjectBeforeUpdateEvent<ContactInfo>, ContactKeyFieldChangedTriggerHandler>();
            parameters.Services.AddInfoObjectEventHandler<InfoObjectAfterUpdateEvent<ContactInfo>, ContactKeyFieldChangedTriggerHandler>();
        }


        /// <summary>
        /// Initializes the module.
        /// </summary>
        protected override void OnInit(ModuleInitParameters parameters)
        {
            base.OnInit(parameters);

            NotificationEmailPlaceholderConfigurationStore.Instance.TryAdd(new AutomationProcessNotificationPlaceholders());

            InitializeSamples(parameters);
        }


        /// <summary>
        /// Registers sample personal data collectors immediately or attaches an event handler to register the collectors upon dedicated key insertion.
        /// Disabling or toggling registration of the sample collectors is not supported.
        /// </summary>
        private static void InitializeSamples(ModuleInitParameters parameters)
        {
            var settingsKeyInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<SettingsKeyInfo>>();

            var dataProtectionSamplesEnabledSettingsKey = settingsKeyInfoProvider.Get(DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME);
            if (dataProtectionSamplesEnabledSettingsKey?.KeyValue.ToBoolean(false) ?? false)
            {
                RegisterDataProtectionSample(parameters);
            }
            else
            {
                SettingsKeyInfo.TYPEINFO.Events.Insert.After += (sender, eventArgs) =>
                {
                    var settingKey = eventArgs.Object as SettingsKeyInfo;
                    if (settingKey.KeyName.Equals(DATA_PROTECTION_SAMPLES_ENABLED_SETTINGS_KEY_NAME, StringComparison.OrdinalIgnoreCase)
                        && settingKey.KeyValue.ToBoolean(false))
                    {
                        RegisterDataProtectionSample(parameters);
                    }
                };
            }
        }


        private static void RegisterDataProtectionSample(ModuleInitParameters parameters)
        {
            if (parameters.Services.GetRequiredService<IOptions<CustomerDataPlatformOptions>>().Value.Enabled)
            {
                RegisterProfileSample(parameters);
                return;
            }

            RegisterContactSample(parameters);
        }


        private static void RegisterContactSample(ModuleInitParameters parameters)
        {
            var contactInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ContactInfo>>();
            var memberInfoProvider = parameters.Services.GetRequiredService<IMemberInfoProvider>();
            var activityInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ActivityInfo>>();
            var countryInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<CountryInfo>>();
            var stateInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<StateInfo>>();
            var consentAgreementInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ConsentAgreementInfo>>();
            var bizFormInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<BizFormInfo>>();

            IdentityCollectorRegister.Instance.Add(new SampleContactInfoIdentityCollector(contactInfoProvider));
            IdentityCollectorRegister.Instance.Add(new SampleMemberInfoIdentityCollector(memberInfoProvider));

            PersonalDataCollectorRegister.Instance.Add(new SampleContactDataCollector(activityInfoProvider, countryInfoProvider, stateInfoProvider, consentAgreementInfoProvider, bizFormInfoProvider));
            PersonalDataCollectorRegister.Instance.Add(new SampleMemberDataCollector());

            PersonalDataEraserRegister.Instance.Add(new SampleContactPersonalDataEraser(consentAgreementInfoProvider, bizFormInfoProvider, contactInfoProvider, activityInfoProvider));
            PersonalDataEraserRegister.Instance.Add(new SampleMemberPersonalDataEraser(memberInfoProvider));

            RegisterConsentRevokeHandler(parameters);
        }


        private static void RegisterProfileSample(ModuleInitParameters parameters)
        {
            RegisterProfileIdentityCollector(parameters);
            RegisterProfileDataCollector(parameters);
            RegisterProfileDataEraser(parameters);

            RegisterConsentRevokeHandler(parameters);
        }


        private static void RegisterProfileIdentityCollector(ModuleInitParameters parameters)
        {
            var profileInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ProfileInfo>>();
            var profileReferenceInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ProfileReferenceInfo>>();
            var contactInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ContactInfo>>();
            var customerInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<CustomerInfo>>();
            var memberInfoProvider = parameters.Services.GetRequiredService<IMemberInfoProvider>();
            var identityCollector = new SampleProfileInfoIdentityCollector(profileInfoProvider, contactInfoProvider, customerInfoProvider, memberInfoProvider);

            IdentityCollectorRegister.Instance.Add(identityCollector);
        }


        private static void RegisterProfileDataCollector(ModuleInitParameters parameters)
        {
            var activityInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ActivityInfo>>();
            var countryInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<CountryInfo>>();
            var stateInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<StateInfo>>();
            var consentAgreementInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ConsentAgreementInfo>>();
            var bizFormInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<BizFormInfo>>();
            var customerAddressInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<CustomerAddressInfo>>();
            var orderInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<OrderInfo>>();
            var orderItemInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<OrderItemInfo>>();
            var orderAddressInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<OrderAddressInfo>>();

            var dataCollector = new SampleProfileDataCollector(activityInfoProvider, countryInfoProvider, stateInfoProvider, consentAgreementInfoProvider,
                bizFormInfoProvider, customerAddressInfoProvider, orderInfoProvider, orderItemInfoProvider, orderAddressInfoProvider);

            PersonalDataCollectorRegister.Instance.Add(dataCollector);
        }


        private static void RegisterProfileDataEraser(ModuleInitParameters parameters)
        {
            var consentAgreementInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ConsentAgreementInfo>>();
            var bizFormInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<BizFormInfo>>();
            var activityInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ActivityInfo>>();
            var contactInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ContactInfo>>();
            var memberInfoProvider = parameters.Services.GetRequiredService<IMemberInfoProvider>();
            var profileInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ProfileInfo>>();

            var profilePersonalDataEraser = new SampleProfilePersonalDataEraser(
                consentAgreementInfoProvider,
                bizFormInfoProvider,
                contactInfoProvider,
                activityInfoProvider,
                memberInfoProvider,
                profileInfoProvider);

            PersonalDataEraserRegister.Instance.Add(profilePersonalDataEraser);
        }


        private static void DeleteContactActivities(ContactInfo contact, ModuleInitParameters parameters)
        {
            var configuration = new Dictionary<string, object>
            {
                { "deleteActivities", true }
            };

            var contactInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ContactInfo>>();
            var activityInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ActivityInfo>>();
            var consentAgreementInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<ConsentAgreementInfo>>();
            var bizFormInfoProvider = parameters.Services.GetRequiredService<IInfoProvider<BizFormInfo>>();

            new SampleContactPersonalDataEraser(consentAgreementInfoProvider, bizFormInfoProvider, contactInfoProvider, activityInfoProvider)
                    .Erase([contact], configuration);
        }


        private static void RegisterConsentRevokeHandler(ModuleInitParameters parameters)
        {
            DataProtectionEvents.RevokeConsentAgreement.Execute += (sender, args) =>
            {
                if (args.Consent.ConsentName.Equals(TrackingConsentGenerator.CONSENT_NAME, StringComparison.Ordinal))
                {
                    DeleteContactActivities(args.Contact, parameters);

                    // Remove cookies used for contact tracking
                    var cookieAccessor = parameters.Services.GetRequiredService<ICookieAccessor>();

#pragma warning disable CS0618 // CookieName is obsolete
                    cookieAccessor.Remove(CookieName.CurrentContact);
                    cookieAccessor.Remove(CookieName.CrossSiteContact);
#pragma warning restore CS0618 // CookieName is obsolete


                    // Set the cookie level to default
                    var cookieLevelProvider = parameters.Services.GetRequiredService<ICurrentCookieLevelProvider>();
                    cookieLevelProvider.SetCurrentCookieLevel(cookieLevelProvider.GetDefaultCookieLevel());
                }
            };
        }
    }
}
