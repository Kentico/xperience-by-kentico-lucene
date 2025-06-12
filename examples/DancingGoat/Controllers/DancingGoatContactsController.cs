using System.Linq;
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
        private readonly IContentRetriever contentRetriever;

        public DancingGoatContactsController(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }

        public async Task<IActionResult> Index()
        {
            var contactsPage = await contentRetriever.RetrieveCurrentPage<ContactsPage>();

            var cafes = await contentRetriever.RetrieveContent<Cafe>();

            var contact = (await contentRetriever.RetrieveContent<Contact>(
                HttpContext.RequestAborted
            )).FirstOrDefault();

            var companyCafes = cafes.Where(c => c.CafeIsCompanyCafe).OrderBy(c => c.CafeName).Select(CafeViewModel.GetViewModel).ToList();
            var partnerCafes = cafes.Where(c => !c.CafeIsCompanyCafe).OrderBy(c => c.CafeCity).Select(CafeViewModel.GetViewModel).ToList();

            var model = new ContactsIndexViewModel
            {
                WebPage = contactsPage,
                CompanyContact = ContactViewModel.GetViewModel(contact),
                CompanyCafes = companyCafes,
                PartnerCafes = partnerCafes
            };

            return View(model);
        }
    }
}
