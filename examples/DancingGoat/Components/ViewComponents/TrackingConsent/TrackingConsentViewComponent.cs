using System.Threading.Tasks;

using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Helpers.Generator;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class TrackingConsentViewComponent : ViewComponent
    {
        private readonly IInfoProvider<ConsentInfo> consentInfoProvider;
        private readonly IConsentAgreementService consentAgreementService;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IWebPageUrlRetriever urlRetriever;
        private readonly IWebsiteChannelContext websiteChannelContext;


        public TrackingConsentViewComponent(
            IInfoProvider<ConsentInfo> consentInfoProvider,
            IConsentAgreementService consentAgreementService,
            IPreferredLanguageRetriever currentLanguageRetriever,
            IWebPageDataContextRetriever webPageDataContextRetriever,
            IWebPageUrlRetriever urlRetriever,
            IWebsiteChannelContext websiteChannelContext)
        {
            this.consentInfoProvider = consentInfoProvider;
            this.consentAgreementService = consentAgreementService;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.urlRetriever = urlRetriever;
            this.websiteChannelContext = websiteChannelContext;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var consent = consentInfoProvider.Get(TrackingConsentGenerator.CONSENT_NAME);

            if (consent != null)
            {
                var currentLanguage = currentLanguageRetriever.Get();
                var consentModel = new ConsentViewModel
                {
                    ConsentShortText = (await consent.GetConsentTextAsync(currentLanguage)).ShortText,
                    ReturnPageUrl = webPageDataContextRetriever.TryRetrieve(out var currentWebPageContext)
                        ? (await urlRetriever.Retrieve(currentWebPageContext.WebPage.WebPageItemID, currentLanguage, cancellationToken: HttpContext.RequestAborted)).RelativePath
                        : (HttpContext.Request.PathBase + HttpContext.Request.Path).Value
                };

                var contact = ContactManagementContext.CurrentContact;
                if ((contact != null) && consentAgreementService.IsAgreed(contact, consent))
                {
                    consentModel.IsConsentAgreed = true;
                    consentModel.PrivacyPageUrl = Url.Content((await urlRetriever.Retrieve(PrivacyPageConstants.PRIVACY_PAGE_TREE_PATH, websiteChannelContext.WebsiteChannelName, currentLanguage, cancellationToken: HttpContext.RequestAborted)).RelativePath);
                }

                return View("~/Components/ViewComponents/TrackingConsent/Default.cshtml", consentModel);
            }

            return Content(string.Empty);
        }
    }
}
