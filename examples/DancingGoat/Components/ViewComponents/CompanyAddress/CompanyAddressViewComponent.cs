using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class CompanyAddressViewComponent : ViewComponent
    {
        private readonly IContentRetriever contentRetriever;


        public CompanyAddressViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var contact = (await contentRetriever.RetrieveContent<Contact>(
                HttpContext.RequestAborted
            )).FirstOrDefault();

            var model = ContactViewModel.GetViewModel(contact);

            return View("~/Components/ViewComponents/CompanyAddress/Default.cshtml", model);
        }
    }
}
