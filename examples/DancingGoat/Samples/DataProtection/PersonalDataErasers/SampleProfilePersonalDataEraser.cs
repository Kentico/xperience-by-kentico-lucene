using System.Collections.Generic;
using System.Linq;

using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using CMS.Membership;
using CMS.OnlineForms;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Sample implementation of <see cref="IPersonalDataEraser"/> interface for erasing profile's personal data.
    /// </summary>
    internal class SampleProfilePersonalDataEraser : IPersonalDataEraser
    {
        private readonly SampleContactPersonalDataEraser contactPersonalDataEraser;
        private readonly SampleMemberPersonalDataEraser memberPersonalDataEraser;
        private readonly IInfoProvider<ProfileInfo> profileInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleProfilePersonalDataEraser"/> class.
        /// </summary>
        /// <param name="consentAgreementInfoProvider">Consent agreement info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provider.</param>
        /// <param name="contactInfoProvider">Contact info provider.</param>
        /// <param name="activityInfoProvider">Activity info provider.</param>
        /// <param name="memberInfoProvider">Member info provider.</param>
        /// <param name="profileInfoProvider">Profile info provider.</param>
        public SampleProfilePersonalDataEraser(
            IInfoProvider<ConsentAgreementInfo> consentAgreementInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider,
            IInfoProvider<ContactInfo> contactInfoProvider,
            IInfoProvider<ActivityInfo> activityInfoProvider,
            IMemberInfoProvider memberInfoProvider,
            IInfoProvider<ProfileInfo> profileInfoProvider)
        {
            contactPersonalDataEraser = new SampleContactPersonalDataEraser(consentAgreementInfoProvider, bizFormInfoProvider, contactInfoProvider, activityInfoProvider);
            memberPersonalDataEraser = new SampleMemberPersonalDataEraser(memberInfoProvider);
            this.profileInfoProvider = profileInfoProvider;
        }


        /// <summary>
        /// Erases personal data based on given <paramref name="identities"/> and <paramref name="configuration"/>.
        /// </summary>
        /// <param name="identities">Collection of identities representing a data subject.</param>
        /// <param name="configuration">Configures which personal data should be erased.</param>
        /// <remarks>
        /// Customer data are not erased as there is a legal interest to keep them.
        /// </remarks>
        /// <remarks>
        /// The erasure process can be configured via the following <paramref name="configuration"/> parameters:
        /// <list type="bullet">
        /// <item>
        /// <term>DeleteMembers</term>
        /// <description>Flag indicating whether member(s) contained in <paramref name="identities"/> are to be deleted.</description>
        /// </item>
        /// <item>
        /// <term>DeleteProfile</term>
        /// <description>Flag indicating whether profile(s) contained in <paramref name="identities"/> are to be deleted. When set to true, all related data will be deleted.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public void Erase(IEnumerable<BaseInfo> identities, IDictionary<string, object> configuration)
        {
            var profiles = identities.OfType<ProfileInfo>();
            if (!profiles.Any())
            {
                return;
            }

            // Delegate to specific erasers for members and contacts
            contactPersonalDataEraser.Erase(identities, configuration);
            memberPersonalDataEraser.Erase(identities, configuration);
            EraseProfile(profiles, configuration);
        }


        /// <summary>
        /// Deletes profile(s) based on <paramref name="configuration"/>'s <c>DeleteProfile</c> flag.
        /// </summary>
        private void EraseProfile(IEnumerable<ProfileInfo> profiles, IDictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("DeleteProfile", out object deleteProfile)
                && ValidationHelper.GetBoolean(deleteProfile, false))
            {
                foreach (var profile in profiles)
                {
                    profileInfoProvider.Delete(profile);
                }
            }
        }
    }
}
