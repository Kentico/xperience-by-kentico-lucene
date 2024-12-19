using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Cafe card section view component.
    /// </summary>
    public class CafeCardSectionViewComponent : ViewComponent
    {
        private readonly ContactsPageRepository contactsPageRepository;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        public CafeCardSectionViewComponent(IPreferredLanguageRetriever currentLanguageRetriever, ContactsPageRepository contactsPageRepository, IWebPageUrlRetriever webPageUrlRetriever)
        {
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.contactsPageRepository = contactsPageRepository;
            this.webPageUrlRetriever = webPageUrlRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(IEnumerable<CafeViewModel> cafes)
        {
            string languageName = currentLanguageRetriever.Get();
            string contactsPagePath = await GetContactsPagePath(languageName, HttpContext.RequestAborted);
            var model = new CafeCardSectionViewModel(cafes, contactsPagePath);

            return View("~/Components/ViewComponents/CafeCardSection/Default.cshtml", model);
        }


        private async Task<string> GetContactsPagePath(string languageName, CancellationToken cancellationToken)
        {
            const string CONTACTS_PAGE_TREE_PATH = "/Contacts";

            var contactsPage = await contactsPageRepository.GetContactsPage(CONTACTS_PAGE_TREE_PATH, languageName, cancellationToken);
            var url = await webPageUrlRetriever.Retrieve(contactsPage, languageName, cancellationToken);

            return url.RelativePath;
        }
    }
}
