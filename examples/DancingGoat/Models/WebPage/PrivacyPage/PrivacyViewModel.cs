using System.Collections.Generic;
using System.Linq;

using CMS.Websites;

namespace DancingGoat.Models
{
    public class PrivacyViewModel : IWebPageBasedViewModel
    {
        public bool DemoDisabled { get; set; }


        public bool ShowSavedMessage { get; set; }


        public bool ShowErrorMessage { get; set; }


        public IEnumerable<PrivacyConsentViewModel> Consents { get; set; } = Enumerable.Empty<PrivacyConsentViewModel>();


        public string PrivacyPageUrl { get; set; }


        public IWebPageFieldsSource WebPage { get; init; }
    }
}
