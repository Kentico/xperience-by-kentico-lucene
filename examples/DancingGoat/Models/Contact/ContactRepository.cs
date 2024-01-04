using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of contact information.
    /// </summary>
    public class ContactRepository
    {
        private readonly IPageRetriever pageRetriever;


        /// <summary>
        /// Initializes a new instance of the <see cref="ContactRepository"/> class that returns contact information.
        /// </summary>
        /// <param name="pageRetriever">Retriever for pages based on given parameters.</param>
        public ContactRepository(IPageRetriever pageRetriever)
        {
            this.pageRetriever = pageRetriever;
        }


        /// <summary>
        /// Returns company's contact information.
        /// </summary>
        public Contact GetCompanyContact()
        {
            return pageRetriever.Retrieve<Contact>(
                query => query
                    .TopN(1),
                cache => cache
                    .Key($"{nameof(ContactRepository)}|{nameof(GetCompanyContact)}"))
                .FirstOrDefault();
        }
    }
}