using System.Linq;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record ConfirmationPageViewModel(string Title, string Header, string Content, WebPageRelatedItem ArticlesSection)
        : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Validates and maps <see cref="ConfirmationPage"/> to a <see cref="ConfirmationPageViewModel"/>.
        /// </summary>
        public static ConfirmationPageViewModel GetViewModel(ConfirmationPage confirmationPage)
        {
            if (confirmationPage == null)
            {
                return null;
            }

            return new ConfirmationPageViewModel(
                confirmationPage.ConfirmationPageTitle,
                confirmationPage.ConfirmationPageHeader,
                confirmationPage.ConfirmationPageContent,
                confirmationPage.ConfirmationPageArticlesSection.FirstOrDefault())
            {
                WebPage = confirmationPage
            };
        }
    }
}
