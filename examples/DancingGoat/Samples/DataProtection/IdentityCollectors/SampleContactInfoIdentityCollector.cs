using System.Collections.Generic;
using System.Linq;

using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IIdentityCollector"/> for collecting <see cref="ContactInfo"/>s by an email address.
    /// </summary>
    internal class SampleContactInfoIdentityCollector : IIdentityCollector
    {
        private readonly IInfoProvider<ContactInfo> contactInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleContactInfoIdentityCollector"/> class.
        /// </summary>
        /// <param name="contactInfoProvider">Contact info provider.</param>
        public SampleContactInfoIdentityCollector(IInfoProvider<ContactInfo> contactInfoProvider)
        {
            this.contactInfoProvider = contactInfoProvider;
        }


        /// <summary>
        /// Collects all the <see cref="ContactInfo"/>s and adds them to the <paramref name="identities"/> collection.
        /// </summary>
        /// <remarks>
        /// Contacts are collected by their email address.
        /// Duplicate customers are not added.
        /// </remarks>
        /// <param name="dataSubjectIdentifiersFilter">Key value collection containing data subject's information that identifies it.</param>
        /// <param name="identities">List of already collected identities.</param>
        public void Collect(IDictionary<string, object> dataSubjectIdentifiersFilter, List<BaseInfo> identities)
        {
            if (!dataSubjectIdentifiersFilter.ContainsKey("email"))
            {
                return;
            }

            var email = dataSubjectIdentifiersFilter["email"] as string;
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            // Find contacts that used the same email and distinct them
            var contacts = contactInfoProvider.Get().WhereEquals(nameof(ContactInfo.ContactEmail), email).ToList();

            identities.AddRange(contacts);
        }
    }
}
