using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Cafe card section view component.
    /// </summary>
    public class CafeCardSectionViewComponent : ViewComponent
    {
        private readonly IContentRetriever contentRetriever;

        public CafeCardSectionViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(IEnumerable<CafeViewModel> cafes)
        {
            string contactsPagePath = await GetContactsPagePath(HttpContext.RequestAborted);
            var model = new CafeCardSectionViewModel(cafes, contactsPagePath);

            return View("~/Components/ViewComponents/CafeCardSection/Default.cshtml", model);
        }


        private async Task<string> GetContactsPagePath(CancellationToken cancellationToken)
        {
            var contactsPage = (await contentRetriever.RetrievePages<ContactsPage>(
                RetrievePagesParameters.Default,
                query => query.UrlPathColumns(),
                new RetrievalCacheSettings("UrlPathColumns"),
                cancellationToken
            )).FirstOrDefault();

            var url = contactsPage.GetUrl();

            return url.RelativePath;
        }
    }
}
