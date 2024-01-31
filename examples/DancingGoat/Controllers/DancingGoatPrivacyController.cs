using System.Collections.Generic;
using System.Linq;

using CMS.ContactManagement;
using CMS.DataProtection;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Helpers.Generator;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(PrivacyPage.CONTENT_TYPE_NAME, typeof(DancingGoatPrivacyController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatPrivacyController : Controller
    {
        private const string SUCCESS_RESULT = "success";
        private const string ERROR_RESULT = "error";

        private readonly IConsentAgreementService consentAgreementService;
        private readonly IConsentInfoProvider consentInfoProvider;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private ContactInfo currentContact;


        private ContactInfo CurrentContact
        {
            get
            {
                if (currentContact == null)
                {
                    currentContact = ContactManagementContext.CurrentContact;
                }

                return currentContact;
            }
        }


        public DancingGoatPrivacyController(IConsentAgreementService consentAgreementService, IConsentInfoProvider consentInfoProvider, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.consentAgreementService = consentAgreementService;
            this.consentInfoProvider = consentInfoProvider;
            this.currentLanguageRetriever = currentLanguageRetriever;
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
                model.Consents = GetAgreedConsentsForCurrentContact();
            }

            model.ShowSavedMessage = TempData[SUCCESS_RESULT] != null;
            model.ShowErrorMessage = TempData[ERROR_RESULT] != null;
            model.PrivacyPageUrl = HttpContext.Request.Path;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/Revoke")]
        public ActionResult Revoke(string returnUrl, string consentName)
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

            return Redirect(returnUrl);
        }


        private IEnumerable<PrivacyConsentViewModel> GetAgreedConsentsForCurrentContact()
        {
            return consentAgreementService.GetAgreedConsents(CurrentContact)
                .Select(consent => new PrivacyConsentViewModel
                {
                    Name = consent.Name,
                    Title = consent.DisplayName,
                    Text = consent.GetConsentText(currentLanguageRetriever.Get()).ShortText
                });
        }


        private bool IsDemoEnabled()
        {
            return consentInfoProvider.Get(TrackingConsentGenerator.CONSENT_NAME) != null;
        }
    }
}
