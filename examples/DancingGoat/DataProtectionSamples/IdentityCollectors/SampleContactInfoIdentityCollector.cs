using System;
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
            if (String.IsNullOrWhiteSpace(email))
            {
                return;
            }

            // Find contacts that used the same email and distinct them
            var contacts = ContactInfo.Provider.Get()
                                                .WhereEquals(nameof(ContactInfo.ContactEmail), email)
                                                .ToList();

            identities.AddRange(contacts);
        }
    }
}
