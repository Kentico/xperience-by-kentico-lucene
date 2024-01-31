using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ContactsPage.CONTENT_TYPE_NAME, typeof(DancingGoatContactsController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatContactsController : Controller
    {
        private readonly ContactRepository contactRepository;
        private readonly CafeRepository cafeRepository;


        public DancingGoatContactsController(ContactRepository contactRepository,
            CafeRepository cafeRepository)
        {
            this.contactRepository = contactRepository;
            this.cafeRepository = cafeRepository;
        }


        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await GetIndexViewModel(cancellationToken);

            return View(model);
        }


        private async Task<ContactsIndexViewModel> GetIndexViewModel(CancellationToken cancellationToken)
        {
            var cafes = await cafeRepository.GetCompanyCafes(4, cancellationToken);
            var contact = await contactRepository.GetContact(HttpContext.RequestAborted);

            return new ContactsIndexViewModel
            {
                CompanyContact = ContactViewModel.GetViewModel(contact),
                CompanyCafes = GetCompanyCafesModel(cafes)
            };
        }


        private List<CafeViewModel> GetCompanyCafesModel(IEnumerable<Cafe> cafes)
        {
            return cafes.Select(cafe => CafeViewModel.GetViewModel(cafe)).ToList();
        }
    }
}