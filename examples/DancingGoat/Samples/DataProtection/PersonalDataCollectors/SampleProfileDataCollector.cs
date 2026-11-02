using System.Collections.Generic;

using CMS.Activities;
using CMS.Commerce;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Globalization;
using CMS.OnlineForms;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IPersonalDataCollector"/> interface for collecting profile's personal data.
    /// </summary>
    internal class SampleProfileDataCollector : IPersonalDataCollector
    {
        private readonly IInfoProvider<ActivityInfo> activityInfoProvider;
        private readonly IInfoProvider<CountryInfo> countryInfoProvider;
        private readonly IInfoProvider<StateInfo> stateInfoProvider;
        private readonly IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;
        private readonly IInfoProvider<CustomerAddressInfo> customerAddressInfoProvider;
        private readonly IInfoProvider<OrderInfo> orderInfoProvider;
        private readonly IInfoProvider<OrderItemInfo> orderItemInfoProvider;
        private readonly IInfoProvider<OrderAddressInfo> orderAddressInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleProfileDataCollector"/>.
        /// </summary>
        /// <param name="activityInfoProvider">Activity info provider.</param>
        /// <param name="countryInfoProvider">Country info provider.</param>
        /// <param name="stateInfoProvider">State info provider.</param>
        /// <param name="consentAgreementInfoProvider">Consent agreement info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provider.</param>
        /// <param name="customerAddressInfoProvider">Customer address info provider.</param>
        /// <param name="orderInfoProvider">Order info provider.</param>
        /// <param name="orderItemInfoProvider">Order item info provider.</param>
        /// <param name="orderAddressInfoProvider">Order address info provider.</param>
        public SampleProfileDataCollector(
            IInfoProvider<ActivityInfo> activityInfoProvider,
            IInfoProvider<CountryInfo> countryInfoProvider,
            IInfoProvider<StateInfo> stateInfoProvider,
            IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider,
            IInfoProvider<CustomerAddressInfo> customerAddressInfoProvider,
            IInfoProvider<OrderInfo> orderInfoProvider,
            IInfoProvider<OrderItemInfo> orderItemInfoProvider,
            IInfoProvider<OrderAddressInfo> orderAddressInfoProvider)
        {
            this.activityInfoProvider = activityInfoProvider;
            this.countryInfoProvider = countryInfoProvider;
            this.stateInfoProvider = stateInfoProvider;
            this.consentAgreementInfoProvider = consentAgreementInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
            this.customerAddressInfoProvider = customerAddressInfoProvider;
            this.orderInfoProvider = orderInfoProvider;
            this.orderItemInfoProvider = orderItemInfoProvider;
            this.orderAddressInfoProvider = orderAddressInfoProvider;
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
                var contactDataCollector = new SampleContactDataCollectorCore(activityInfoProvider, countryInfoProvider, stateInfoProvider, consentAgreementInfoProvider, bizFormInfoProvider);
                contactDataCollector.CollectData(identities, writer);

                var memberDataCollector = new SampleMemberDataCollectorCore();
                memberDataCollector.CollectData(identities, writer);

                var customerDataCollector = new SampleCustomerDataCollectorCore(customerAddressInfoProvider, countryInfoProvider, stateInfoProvider, orderInfoProvider, orderItemInfoProvider, orderAddressInfoProvider);
                customerDataCollector.CollectData(identities, writer);

                return new PersonalDataCollectorResult
                {
                    Text = writer.GetResult()
                };
            }
        }


        private static IPersonalDataWriter CreateWriter(string outputFormat)
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
