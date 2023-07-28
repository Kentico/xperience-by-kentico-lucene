using CMS.DocumentEngine;

using Kentico.Xperience.Lucene.Attributes;
using Lucene.Net.Documents;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// The base class for all Lucene search models. Contains common Lucene
    /// fields which should be present in all indexes.
    /// </summary>
    public class LuceneSearchModel
    {
        /// <summary>
        /// The internal Lucene ID of this search record.
        /// </summary>
        [StringField(true)]
        public string? ObjectID
        {
            get;
            set;
        }


        /// <summary>
        /// The name of the Xperience class to which the indexed data belongs.
        /// </summary>
        [StringField(true)]
        public string? ClassName
        {
            get;
            set;
        }


        /// <summary>
        /// The absolute live site URL of the indexed page.
        /// </summary>
        [StringField(true)]
        public string? Url
        {
            get;
            set;
        }


        /// <summary>
        /// Called when indexing a search model property. Does not trigger when indexing the
        /// properties specified by <see cref="LuceneSearchModel"/> base class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> currently being indexed.</param>
        /// <param name="propertyName">The search model property that is being indexed.</param>
        /// <param name="usedColumn">The column that the value was retrieved from when the
        /// property uses the <see cref="SourceAttribute"/>. If not used, the parameter will
        /// be null.</param>
        /// <param name="foundValue">The value of the property that was found in the <paramref name="node"/>,
        /// or null if no value was found.</param>
        /// <returns>The value that will be indexed in Lucene.</returns>
        public virtual Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
        => Task.FromResult(foundValue);

        /// <summary>
        /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> currently being indexed.</param>
        /// <param name="document">The resulting Lucene document to be modified. The document could be changed during the process</param>
        /// <returns>Modified Lucene document.</returns>
        public virtual Task<Document> OnIndexingDocument(TreeNode node, Document document)
        => Task.FromResult(document);
    }
}
