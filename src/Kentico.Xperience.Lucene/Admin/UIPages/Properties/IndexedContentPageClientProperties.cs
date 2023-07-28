using Kentico.Xperience.Admin.Base;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// Template properties for the <see cref="IndexedContent"/> UI page.
    /// </summary>
    internal class IndexedContentPageClientProperties : TemplateClientProperties
    {
        /// <summary>
        /// Columns to display in the indexed path table.
        /// </summary>
        public IEnumerable<Column>? PathColumns
        {
            get;
            set;
        }


        /// <summary>
        /// Rows to display in the indexed path table.
        /// </summary>
        public IEnumerable<Row>? PathRows
        {
            get;
            set;
        }


        /// <summary>
        /// Columns to display in the indexed properties table.
        /// </summary>
        public IEnumerable<Column>? PropertyColumns
        {
            get;
            set;
        }


        /// <summary>
        /// Rows to display in the indexed properties table.
        /// </summary>
        public IEnumerable<Row>? PropertyRows
        {
            get;
            set;
        }
    }
}
