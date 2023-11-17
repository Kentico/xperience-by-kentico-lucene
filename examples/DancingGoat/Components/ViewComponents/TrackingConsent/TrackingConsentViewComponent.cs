using CMS.DataProtection;

using DancingGoat.Helpers.Generator;
using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class TrackingConsentViewComponent : ViewComponent
    {
        private readonly IConsentInfoProvider consentInfoProvider;
        private readonly IConsentAgreementService consentAgreementService;


        public TrackingConsentViewComponent(IConsentInfoProvider consentInfoProvider, IConsentAgreementService consentAgreementService)
        {
            this.consentInfoProvider = consentInfoProvider;
            this.consentAgreementService = consentAgreementService;
        }


        public IViewComponentResult Invoke()
        {
            var consent = consentInfoProvider.Get(TrackingConsentGenerator.CONSENT_NAME);

            if (consent != null)
            {
                var consentModel = new ConsentViewModel
                {
                    ConsentShortText = consent.GetConsentText(System.Threading.Thread.CurrentThread.CurrentUICulture.Name).ShortText
                };

                var contact = CMS.ContactManagement.ContactManagementContext.CurrentContact;
                if ((contact != null) && consentAgreementService.IsAgreed(contact, consent))
                {
                    consentModel.IsConsentAgreed = true;
                }

                return View("~/Components/ViewComponents/TrackingConsent/_TrackingConsent.cshtml", consentModel);
            }

            return Content(string.Empty);
        }
    }
}
