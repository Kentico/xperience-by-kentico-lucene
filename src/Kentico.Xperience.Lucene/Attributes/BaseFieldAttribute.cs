namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute which specifies the LuceneIndex field type. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class BaseFieldAttribute : Attribute
    {
        /// <summary>
        /// Store the original field value in the index. this is useful for short texts
        /// like a document's title which should be displayed with the results. The
        /// value is stored in its original form, i.e. no analyzer is used before it is
        /// stored.
        /// </summary>
        public bool Store
        {
            get;
            protected set;
        }
    }
}
