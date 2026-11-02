using System.Collections.Generic;
using System.Linq;

using CMS.DataEngine;
using CMS.Membership;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Class responsible for retrieving members's personal data. 
    /// </summary>
    internal class SampleMemberDataCollectorCore
    {
        // Lists store Tuples of database column names and their corresponding display names.
        private readonly List<CollectedColumn> memberInfoColumns = new List<CollectedColumn> {
            new(nameof(MemberInfo.MemberName), "Name"),
            new(nameof(MemberInfo.MemberEmail), "Email"),
            new(nameof(MemberInfo.MemberEnabled), "Enabled"),
            new(nameof(MemberInfo.MemberIsExternal), "Is external"),
            new(nameof(MemberInfo.MemberCreated), "Created"),
            new(nameof(MemberInfo.MemberGuid), "GUID"),
        };


        /// <summary>
        /// Collect and format all member personal data about given <paramref name="identities"/>.
        /// </summary>
        /// <param name="identities">Identities to collect data about.</param>
        /// <param name="writer">Writer to format output data.</param>
        public void CollectData(IEnumerable<BaseInfo> identities, IPersonalDataWriter writer)
        {
            var memberInfos = identities.OfType<MemberInfo>().ToList();
            if (!memberInfos.Any())
            {
                return;
            }

            writer.WriteStartSection("MemberData", "Member data");

            foreach (var memberInfo in memberInfos)
            {
                WriteMemberInfo(memberInfo, writer);
            }

            writer.WriteEndSection();
        }


        /// <summary>
        /// Writes base info for given member to the current writer.
        /// </summary>
        /// <param name="memberInfo">Member info object.</param>
        /// <param name="writer">Writer to format output data.</param>
        private void WriteMemberInfo(MemberInfo memberInfo, IPersonalDataWriter writer)
        {
            writer.WriteStartSection(MemberInfo.OBJECT_TYPE, "Member");
            writer.WriteBaseInfo(memberInfo, memberInfoColumns);
            writer.WriteEndSection();
        }
    }
}
