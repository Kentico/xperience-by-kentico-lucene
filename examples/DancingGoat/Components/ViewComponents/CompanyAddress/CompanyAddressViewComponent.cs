using System.Threading.Tasks;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class CompanyAddressViewComponent : ViewComponent
    {
        private readonly ContactRepository contactRepository;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        public CompanyAddressViewComponent(ContactRepository contactRepository, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.contactRepository = contactRepository;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var contact = await contactRepository.GetContact();
            var model = ContactViewModel.GetViewModel(contact);

            return View("~/Components/ViewComponents/CompanyAddress/Default.cshtml", model);
        }
    }
}
