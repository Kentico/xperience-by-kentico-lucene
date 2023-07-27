namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute which specifies the LuceneIOndex field type.
    /// A field that is indexed but not tokenized: the entire
    /// <see cref="string"/> value is indexed as a single token.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StringFieldAttribute : BaseFieldAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringFieldAttribute"/> class.
        /// </summary>
        /// <param name="store">Store the original field value in the index. this is useful for short texts
        /// like a document's title which should be displayed with the results. The
        /// value is stored in its original form, i.e. no analyzer is used before it is
        /// stored.</param>
        public StringFieldAttribute(bool store = false) => Store = store;
    }
}
