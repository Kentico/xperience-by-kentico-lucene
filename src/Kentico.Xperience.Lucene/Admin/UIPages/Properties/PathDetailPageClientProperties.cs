using Kentico.Xperience.Admin.Base;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// Template properties for the <see cref="PathDetail"/> UI page.
    /// </summary>
    internal class PathDetailPageClientProperties : TemplateClientProperties
    {
        /// <summary>
        /// The alias path being displayed.
        /// </summary>
        public string? AliasPath
        {
            get;
            set;
        }


        /// <summary>
        /// The columns to display in the content type table.
        /// </summary>
        public IEnumerable<Column>? Columns
        {
            get;
            set;
        }
    }
}
