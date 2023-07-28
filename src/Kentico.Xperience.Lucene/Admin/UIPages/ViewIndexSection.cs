using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// A navigation section containing pages for viewing index information.
    /// </summary>
    internal class ViewIndexSection : SecondaryMenuSectionPage
    {
        /// <summary>
        /// The internal <see cref="LuceneIndex.Identifier"/> of the index.
        /// </summary>
        [PageParameter(typeof(IntPageModelBinder))]
        public int IndexIdentifier
        {
            get;
            set;
        }


        /// <inheritdoc/>
        public override Task<TemplateClientProperties> ConfigureTemplateProperties(TemplateClientProperties properties)
        {
            var index = IndexStore.Instance.GetIndex(IndexIdentifier);
            if (index != null)
            {
                properties.Breadcrumbs.Label = index.IndexName;
                properties.Breadcrumbs.IsSignificant = true;
                properties.Navigation.Headline = index.IndexName;
                properties.Navigation.IsSignificant = true;

                return base.ConfigureTemplateProperties(properties);
            }
            throw new InvalidOperationException($"Unable to retrieve Lucene index with identifier '{IndexIdentifier}.'");
        }
    }
}
