using System.Collections.Generic;
using System.Linq;

using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Membership;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IIdentityCollector"/> for collecting <see cref="MemberInfo"/>s by an email address.
    /// </summary>
    internal class SampleMemberInfoIdentityCollector : IIdentityCollector
    {
        private readonly IMemberInfoProvider memberInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleMemberInfoIdentityCollector"/> class.
        /// </summary>
        /// <param name="memberInfoProvider">Member info provider.</param>
        public SampleMemberInfoIdentityCollector(IMemberInfoProvider memberInfoProvider)
        {
            this.memberInfoProvider = memberInfoProvider;
        }


        /// <summary>
        /// Collects all the <see cref="MemberInfo"/>s and adds them to the <paramref name="identities"/> collection.
        /// </summary>
        /// <remarks>
        /// Members are collected by their email address.
        /// </remarks>
        /// <param name="dataSubjectIdentifiersFilter">Key value collection containing data subject's information that identifies it.</param>
        /// <param name="identities">List of already collected identities.</param>
        public void Collect(IDictionary<string, object> dataSubjectIdentifiersFilter, List<BaseInfo> identities)
        {
            if (!dataSubjectIdentifiersFilter.TryGetValue(PersonalDataConstants.DATA_SUBJECT_IDENTIFIER_KEY, out object value))
            {
                return;
            }

            var dataSubjectIdentifier = value as string;
            if (string.IsNullOrWhiteSpace(dataSubjectIdentifier))
            {
                return;
            }

            // Find members that used the same email and distinct them
            var members = memberInfoProvider.Get().WhereEquals(nameof(MemberInfo.MemberEmail), dataSubjectIdentifier).ToList();

            identities.AddRange(members);
        }
    }
}
