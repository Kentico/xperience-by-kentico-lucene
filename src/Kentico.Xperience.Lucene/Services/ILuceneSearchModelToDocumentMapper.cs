using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;

namespace Kentico.Xperience.Lucene.Services
{
    public interface ILuceneSearchModelToDocumentMapper
    {
        /// <summary>
        /// Handles mapping from <see cref="LuceneSearchModel"/>
        /// </summary>
        /// <param name="luceneIndex">The <see cref="LuceneIndex"/>. Could be used to implement changes based on index name.</param>
        /// <param name="model">The <see cref="LuceneSearchModel"/> with data.</param>
        /// <returns>Modified Lucene document.</returns>
        Document MapModelToDocument(LuceneIndex luceneIndex, LuceneSearchModel model);
    }
}