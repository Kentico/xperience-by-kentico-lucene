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
            new CollectedColumn("MemberName", "Name"),
            new CollectedColumn("MemberIsExternal", "Is external"),
            new CollectedColumn("MemberEmail", "Email"),
            new CollectedColumn("MemberEnabled", "Enabled"),
            new CollectedColumn("MemberCreated", "Created"),
            new CollectedColumn("MemberID", "ID"),
        };


        private readonly IPersonalDataWriter writer;


        /// <summary>
        /// Constructs a new instance of the <see cref="SampleMemberDataCollectorCore"/>.
        /// </summary>
        /// <param name="writer">Writer to format output data.</param>
        public SampleMemberDataCollectorCore(IPersonalDataWriter writer)
        {
            this.writer = writer;
        }


        /// <summary>
        /// Collect and format all member personal data about given <paramref name="identities"/>.
        /// Returns null if no data was found.
        /// </summary>
        /// <param name="identities">Identities to collect data about.</param>
        /// <returns>Formatted personal data.</returns>
        public string CollectData(IEnumerable<BaseInfo> identities)
        {
            var memberInfos = identities.OfType<MemberInfo>().ToList();
            if (!memberInfos.Any())
            {
                return null;
            }

            writer.WriteStartSection("MemberData", "Member data");

            foreach (var memberInfo in memberInfos)
            {
                WriteMemberInfo(memberInfo);
            }

            writer.WriteEndSection();

            return writer.GetResult();
        }


        /// <summary>
        /// Writes base info for given member to the current writer.
        /// </summary>
        private void WriteMemberInfo(MemberInfo memberInfo)
        {
            writer.WriteStartSection(MemberInfo.OBJECT_TYPE, "Member");
            writer.WriteBaseInfo(memberInfo, memberInfoColumns);
            writer.WriteEndSection();
        }
    }
}
