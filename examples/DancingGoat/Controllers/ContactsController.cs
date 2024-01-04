using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

[assembly: RegisterPageRoute(Contacts.CLASS_NAME, typeof(ContactsController))]

namespace DancingGoat.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ContactRepository contactRepository;
        private readonly CafeRepository cafeRepository;
        private readonly IStringLocalizer<SharedResources> localizer;


        public ContactsController(ContactRepository contactRepository,
            CafeRepository cafeRepository,
            IStringLocalizer<SharedResources> localizer)
        {
            this.contactRepository = contactRepository;
            this.cafeRepository = cafeRepository;
            this.localizer = localizer;
        }


        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await GetIndexViewModel(cancellationToken);

            return View(model);
        }


        private async Task<ContactsIndexViewModel> GetIndexViewModel(CancellationToken cancellationToken)
        {
            var cafes = await cafeRepository.GetCompanyCafes(ContentItemIdentifiers.CAFES, 4, cancellationToken);

            return new ContactsIndexViewModel
            {
                CompanyContact = GetCompanyContactModel(),
                CompanyCafes = GetCompanyCafesModel(cafes)
            };
        }


        private ContactViewModel GetCompanyContactModel()
        {
            return ContactViewModel.GetViewModel(contactRepository.GetCompanyContact(), localizer);
        }


        private List<ContactViewModel> GetCompanyCafesModel(IEnumerable<Cafe> cafes)
        {
            return cafes.Select(cafe => ContactViewModel.GetViewModel(cafe, localizer)).ToList();
        }
    }
}