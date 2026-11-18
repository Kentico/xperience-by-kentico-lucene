using System.Linq;
using System.Threading.Tasks;

using CMS.DataEngine;

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
            var contactsPage = await contentRetriever.RetrieveCurrentPage<ContactsPage>(
                new RetrieveCurrentPageParameters { LinkedItemsMaxLevel = 1 },
                HttpContext.RequestAborted);

            var companyCafes = await contentRetriever.RetrieveContent<Cafe>(
                new RetrieveContentParameters { LinkedItemsMaxLevel = 1 },
                query => query.Columns(
                        nameof(Cafe.CafeName),
                        nameof(Cafe.CafeStreet),
                        nameof(Cafe.CafeCity),
                        nameof(Cafe.CafeCountry),
                        nameof(Cafe.CafeZipCode),
                        nameof(Cafe.CafePhone),
                        nameof(Cafe.CafePhoto),
                        nameof(Cafe.CafeOrder))
                    .Where(where => where.WhereTrue(nameof(Cafe.CafeIsCompanyCafe)))
                    .OrderBy(OrderByColumn.Asc(nameof(Cafe.CafeOrder))),
                new RetrievalCacheSettings($"Columns_CafeContactCardFieldsWithPhoto_CompanyOnly_OrderBy_{nameof(Cafe.CafeOrder)}_Linked1"),
                HttpContext.RequestAborted
            );

            var contact = (await contentRetriever.RetrieveContent<Contact>(
                RetrieveContentParameters.Default,
                query => query.TopN(1),
                new RetrievalCacheSettings("TopN_1"),
                HttpContext.RequestAborted
            )).FirstOrDefault();

            var model = new ContactsIndexViewModel
            {
                WebPage = contactsPage,
                CompanyContact = ContactViewModel.GetViewModel(contact),
                CompanyCafes = companyCafes.Select(CafeViewModel.GetViewModel).ToList()
            };

            return View(model);
        }
    }
}
