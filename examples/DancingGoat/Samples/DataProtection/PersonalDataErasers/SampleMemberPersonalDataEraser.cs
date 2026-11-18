using System.Collections.Generic;
using System.Linq;

using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using CMS.Membership;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IPersonalDataEraser"/> interface for erasing members's personal data.
    /// </summary>
    internal class SampleMemberPersonalDataEraser : IPersonalDataEraser
    {
        private readonly IMemberInfoProvider memberInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleMemberPersonalDataEraser"/> class.
        /// </summary>
        /// <param name="memberInfoProvider">Member info provider.</param>
        public SampleMemberPersonalDataEraser(IMemberInfoProvider memberInfoProvider)
        {
            this.memberInfoProvider = memberInfoProvider;
        }


        /// <summary>
        /// Erases personal data based on given <paramref name="identities"/> and <paramref name="configuration"/>.
        /// </summary>
        /// <param name="identities">Collection of identities representing a data subject.</param>
        /// <param name="configuration">Configures which personal data should be erased.</param>
        /// <remarks>
        /// The erasure process can be configured via the following <paramref name="configuration"/> parameters:
        /// <list type="bullet">
        /// <item>
        /// <term>DeleteMembers</term>
        /// <description>Flag indicating whether member(s) contained in <paramref name="identities"/> are to be deleted.</description>
        /// </item>
        /// <item>
        /// <term>DeleteProfile</term>
        /// <description>Flag indicating whether all profile data should be deleted. When set to true, all related data will be deleted.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public void Erase(IEnumerable<BaseInfo> identities, IDictionary<string, object> configuration)
        {
            var members = identities.OfType<MemberInfo>().ToList();

            DeleteMembers(members, configuration);
        }


        /// <summary>
        /// Deletes all members, based on <paramref name="configuration"/>'s <c>DeleteMembers</c> flag.
        /// </summary>
        private void DeleteMembers(List<MemberInfo> members, IDictionary<string, object> configuration)
        {
            if (ShouldDeleteData(configuration, "deleteMembers"))
            {
                foreach (var member in members)
                {
                    memberInfoProvider.Delete(member);
                }
            }
        }


        /// <summary>
        /// Determines whether data should be deleted based on the specific configuration key or the global DeleteProfile flag.
        /// </summary>
        /// <param name="configuration">Configuration dictionary.</param>
        /// <param name="configKey">Specific configuration key to check.</param>
        /// <returns>True if data should be deleted, false otherwise.</returns>
        private static bool ShouldDeleteData(IDictionary<string, object> configuration, string configKey)
        {
            var areAllDataToBeDeleted = configuration.TryGetValue("DeleteProfile", out object deleteProfile)
                && ValidationHelper.GetBoolean(deleteProfile, false);

            var areSpecificDataToBeDeleted = configuration.TryGetValue(configKey, out object deleteSpecific)
                && ValidationHelper.GetBoolean(deleteSpecific, false);

            return areAllDataToBeDeleted || areSpecificDataToBeDeleted;
        }
    }
}
