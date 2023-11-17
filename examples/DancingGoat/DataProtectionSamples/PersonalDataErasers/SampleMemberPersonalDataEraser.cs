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
        private static void DeleteMembers(List<MemberInfo> members, IDictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("deleteMembers", out object deleteMembers)
                && ValidationHelper.GetBoolean(deleteMembers, false))
            {
                foreach (var member in members)
                {
                    MemberInfo.Provider.Delete(member);
                }
            }
        }
    }
}
