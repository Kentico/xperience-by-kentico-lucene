using System.Threading.Tasks;

using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class CompanyAddressViewComponent : ViewComponent
    {
        private readonly ContactRepository contactRepository;


        public CompanyAddressViewComponent(ContactRepository contactRepository)
        {
            this.contactRepository = contactRepository;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var contact = await contactRepository.GetContact(HttpContext.RequestAborted);
            var model = ContactViewModel.GetViewModel(contact);

            return View("~/Components/ViewComponents/CompanyAddress/Default.cshtml", model);
        }
    }
}
