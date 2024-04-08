using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;

using DancingGoat.Helpers.Generator;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    public class ConsentController : Controller
    {
        private readonly ICurrentCookieLevelProvider cookieLevelProvider;
        private readonly IConsentAgreementService consentAgreementService;
        private readonly IInfoProvider<ConsentInfo> consentInfoProvider;


        public ConsentController(ICurrentCookieLevelProvider cookieLevelProvider, IConsentAgreementService consentAgreementService, IInfoProvider<ConsentInfo> consentInfoProvider)
        {
            this.cookieLevelProvider = cookieLevelProvider;
            this.consentAgreementService = consentAgreementService;
            this.consentInfoProvider = consentInfoProvider;
        }


        // POST: Consent/Agree
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Agree(string returnUrl)
        {
            var consent = consentInfoProvider.Get(TrackingConsentGenerator.CONSENT_NAME);

            if (consent != null)
            {
                cookieLevelProvider.SetCurrentCookieLevel(Kentico.Web.Mvc.CookieLevel.All.Level);

                var contact = ContactManagementContext.CurrentContact;
                if (contact != null)
                {
                    consentAgreementService.Agree(contact, consent);
                }

                return Redirect(returnUrl);
            }

            return new StatusCodeResult(StatusCodes.Status400BadRequest);
        }
    }
}
