using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ContactsPage.CONTENT_TYPE_NAME, typeof(DancingGoatContactsController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatContactsController : Controller
    {
        private readonly ContactsPageRepository contactsPageRepository;
        private readonly ContactRepository contactRepository;
        private readonly CafeRepository cafeRepository;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;


        public DancingGoatContactsController(ContactsPageRepository contactsPageRepository, ContactRepository contactRepository,
            CafeRepository cafeRepository, IPreferredLanguageRetriever currentLanguageRetriever, IWebPageDataContextRetriever webPageDataContextRetriever)
        {
            this.contactsPageRepository = contactsPageRepository;
            this.contactRepository = contactRepository;
            this.cafeRepository = cafeRepository;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
        }


        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var webPage = webPageDataContextRetriever.Retrieve().WebPage;

            var contactsPage = await contactsPageRepository.GetContactsPage(webPage.WebPageItemID, webPage.LanguageName, HttpContext.RequestAborted);

            var model = await GetIndexViewModel(contactsPage, cancellationToken);

            return View(model);
        }


        private async Task<ContactsIndexViewModel> GetIndexViewModel(ContactsPage contactsPage, CancellationToken cancellationToken)
        {
            var languageName = currentLanguageRetriever.Get();
            var cafes = (await cafeRepository.GetCafes(0, languageName, cancellationToken)).ToList();
            var companyCafes = cafes.Where(c => c.CafeIsCompanyCafe).OrderBy(c => c.CafeName);
            var partnerCafes = cafes.Where(c => !c.CafeIsCompanyCafe).OrderBy(c => c.CafeCity);
            var contact = await contactRepository.GetContact(languageName, HttpContext.RequestAborted);

            return new ContactsIndexViewModel
            {
                CompanyContact = ContactViewModel.GetViewModel(contact),
                CompanyCafes = GetCafesModel(companyCafes),
                PartnerCafes = GetCafesModel(partnerCafes),
                WebPage = contactsPage
            };
        }


        private List<CafeViewModel> GetCafesModel(IEnumerable<Cafe> cafes)
        {
            return cafes.Select(cafe => CafeViewModel.GetViewModel(cafe)).ToList();
        }
    }
}
