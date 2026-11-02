using System.Collections.Generic;
using System.Linq;

using CMS.Commerce;
using CMS.ContactManagement;
using CMS.ContactManagement.Internal;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Membership;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IIdentityCollector"/> for collecting <see cref="ProfileInfo"/>s by an identifier.
    /// </summary>
    internal class SampleProfileInfoIdentityCollector : IIdentityCollector
    {
        private readonly IInfoProvider<ProfileInfo> profileInfoProvider;
        private readonly IInfoProvider<ContactInfo> contactInfoProvider;
        private readonly IInfoProvider<CustomerInfo> customerInfoProvider;
        private readonly IMemberInfoProvider memberInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleProfileInfoIdentityCollector"/> class.
        /// </summary>
        /// <param name="profileInfoProvider">Profile info provider.</param>
        /// <param name="contactInfoProvider">Contact info provider.</param>
        /// <param name="customerInfoProvider">Customer info provider.</param>
        /// <param name="memberInfoProvider">Member info provider.</param>
        public SampleProfileInfoIdentityCollector(
            IInfoProvider<ProfileInfo> profileInfoProvider,
            IInfoProvider<ContactInfo> contactInfoProvider,
            IInfoProvider<CustomerInfo> customerInfoProvider,
            IMemberInfoProvider memberInfoProvider)
        {
            this.profileInfoProvider = profileInfoProvider;
            this.contactInfoProvider = contactInfoProvider;
            this.customerInfoProvider = customerInfoProvider;
            this.memberInfoProvider = memberInfoProvider;
        }


        /// <summary>
        /// Collects all the <see cref="ProfileInfo"/>s, <see cref="ContactInfo"/>s, <see cref="MemberInfo"/>s, <see cref="CustomerInfo"/>s and adds them to the <paramref name="identities"/> collection.
        /// </summary>
        /// <param name="dataSubjectIdentifiersFilter">Key value collection containing data subject's information that identifies it.</param>
        /// <param name="identities">List of already collected identities.</param>
        public void Collect(IDictionary<string, object> dataSubjectIdentifiersFilter, List<BaseInfo> identities)
        {
            if (!dataSubjectIdentifiersFilter.TryGetValue(PersonalDataConstants.DATA_SUBJECT_IDENTIFIER_KEY, out object value))
            {
                return;
            }

            var identifier = value as string;
            if (string.IsNullOrEmpty(identifier))
            {
                return;
            }

            var profile = profileInfoProvider.Get().WhereEquals(nameof(ProfileInfo.ProfileName), identifier).SingleOrDefault();
            if (profile != null)
            {
                identities.Add(profile);

                var reference = profile.GetReferenceOrThrow();
                if (reference != null)
                {
                    if (reference.ProfileReferenceContactID > 0)
                    {
                        var contact = contactInfoProvider.Get(reference.ProfileReferenceContactID);
                        if (contact != null)
                        {
                            identities.Add(contact);
                        }
                    }

                    if (reference.ProfileReferenceCustomerID > 0)
                    {
                        var customer = customerInfoProvider.Get(reference.ProfileReferenceCustomerID);
                        if (customer != null)
                        {
                            identities.Add(customer);
                        }
                    }

                    if (reference.ProfileReferenceMemberID > 0)
                    {
                        var member = memberInfoProvider.Get(reference.ProfileReferenceMemberID);
                        if (member != null)
                        {
                            identities.Add(member);
                        }
                    }
                }
            }
        }
    }
}
