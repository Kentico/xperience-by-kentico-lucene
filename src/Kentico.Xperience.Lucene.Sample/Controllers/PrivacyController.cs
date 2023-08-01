using System.Collections.Generic;
using System.Linq;
using System.Threading;

using CMS.ContactManagement;
using CMS.DataProtection;
using CMS.DocumentEngine.Types.DancingGoatCore;
using CMS.Membership;

using DancingGoat.Controllers;
using DancingGoat.Helpers.Generator;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageRoute(Privacy.CLASS_NAME, typeof(PrivacyController))]

namespace DancingGoat.Controllers
{
    public class PrivacyController : Controller
    {
        private readonly IConsentAgreementService consentAgreementService;
        private readonly IConsentInfoProvider consentInfoProvider;
        private ContactInfo currentContact;

        private const string SUCCESS_RESULT = "success";
        private const string ERROR_RESULT = "error";


        private ContactInfo CurrentContact
        {
            get
            {
                if (currentContact == null)
                {
                    // Try to get contact from cookie
                    currentContact = ContactManagementContext.CurrentContact;
                }

                return currentContact;
            }
        }


        public PrivacyController(IConsentAgreementService consentAgreementService, IConsentInfoProvider consentInfoProvider)
        {
            this.consentAgreementService = consentAgreementService;
            this.consentInfoProvider = consentInfoProvider;
        }


        public ActionResult Index()
        {
            var model = new PrivacyViewModel();

            if (!IsDemoEnabled())
            {
                model.DemoDisabled = true;
            }
            else if (CurrentContact != null)
            {
                model.Constents = GetAgreedConsentsForCurrentContact();
            }

            model.ShowSavedMessage = TempData[SUCCESS_RESULT] != null;
            model.ShowErrorMessage = TempData[ERROR_RESULT] != null;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Privacy/Revoke")]
        public ActionResult Revoke(string consentName)
        {
            var consentToRevoke = consentInfoProvider.Get(consentName);

            if (consentToRevoke != null && CurrentContact != null)
            {
                consentAgreementService.Revoke(CurrentContact, consentToRevoke);

                TempData[SUCCESS_RESULT] = true;
            }
            else
            {
                TempData[ERROR_RESULT] = true;
            }

            return Redirect(Url.Kentico().PageUrl(ContentItemIdentifiers.PRIVACY));
        }


        private IEnumerable<PrivacyConsentViewModel> GetAgreedConsentsForCurrentContact()
        {
            return consentAgreementService.GetAgreedConsents(CurrentContact)
                .Select(consent => new PrivacyConsentViewModel
                {
                    Name = consent.Name,
                    Title = consent.DisplayName,
                    Text = consent.GetConsentText(Thread.CurrentThread.CurrentCulture.Name).ShortText
                });
        }


        private bool IsDemoEnabled()
        {
            return consentInfoProvider.Get(TrackingConsentGenerator.CONSENT_NAME) != null;
        }
    }
}
