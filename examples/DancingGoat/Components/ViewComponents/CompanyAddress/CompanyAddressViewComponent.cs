using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace DancingGoat.ViewComponents
{
    public class CompanyAddressViewComponent : ViewComponent
    {
        private readonly ContactRepository contactRepository;
        private readonly IStringLocalizer<SharedResources> localizer;


        public CompanyAddressViewComponent(ContactRepository contactRepository,
            IStringLocalizer<SharedResources> localizer)
        {
            this.contactRepository = contactRepository;
            this.localizer = localizer;
        }


        public IViewComponentResult Invoke()
        {
            var contact = contactRepository.GetCompanyContact();
            var model = ContactViewModel.GetViewModel(contact, localizer);
            
            return View("~/Components/ViewComponents/CompanyAddress/Default.cshtml", model);
        }
    }
}
