using System.Collections.Generic;

using CMS.Activities;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Globalization;
using CMS.OnlineForms;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IPersonalDataCollector"/> interface for collecting contact's personal data.
    /// </summary>
    internal class SampleContactDataCollector : IPersonalDataCollector
    {
        private readonly IInfoProvider<ActivityInfo> activityInfoProvider;
        private readonly IInfoProvider<CountryInfo> countryInfoProvider;
        private readonly IInfoProvider<StateInfo> stateInfoProvider;
        private readonly IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;


        /// <summary>
        /// Constructs a new instance of the <see cref="SampleContactDataCollector"/>.
        /// </summary>
        /// <param name="activityInfoProvider">Activity info provider.</param>
        /// <param name="countryInfoProvider">Country info provider.</param>
        /// <param name="stateInfoProvider">State info provider.</param>
        /// <param name="consentAgreementInfoProvider">Consent agreement info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provider.</param>
        public SampleContactDataCollector(
            IInfoProvider<ActivityInfo> activityInfoProvider,
            IInfoProvider<CountryInfo> countryInfoProvider,
            IInfoProvider<StateInfo> stateInfoProvider,
            IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider)
        {
            this.activityInfoProvider = activityInfoProvider;
            this.countryInfoProvider = countryInfoProvider;
            this.stateInfoProvider = stateInfoProvider;
            this.consentAgreementInfoProvider = consentAgreementInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
        }


        /// <summary>
        /// Collects personal data based on given <paramref name="identities"/>.
        /// </summary>
        /// <param name="identities">Collection of identities representing a data subject.</param>
        /// <param name="outputFormat">Defines an output format for the result.</param>
        /// <returns><see cref="PersonalDataCollectorResult"/> containing personal data.</returns>
        public PersonalDataCollectorResult Collect(IEnumerable<BaseInfo> identities, string outputFormat)
        {
            using (var writer = CreateWriter(outputFormat))
            {
                var dataCollector = new SampleContactDataCollectorCore(writer, activityInfoProvider, countryInfoProvider, stateInfoProvider, consentAgreementInfoProvider, bizFormInfoProvider);
                return new PersonalDataCollectorResult
                {
                    Text = dataCollector.CollectData(identities)
                };
            }
        }


        private IPersonalDataWriter CreateWriter(string outputFormat)
        {
            switch (outputFormat.ToLowerInvariant())
            {
                case PersonalDataFormat.MACHINE_READABLE:
                    return new XmlPersonalDataWriter();

                case PersonalDataFormat.HUMAN_READABLE:
                default:
                    return new HumanReadablePersonalDataWriter();
            }
        }
    }
}
