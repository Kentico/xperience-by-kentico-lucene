using System;

namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute which specifies the LuceneIndex field type. 
    /// A field that is indexed and tokenized, without term vectors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TextFieldAttribute : BaseFieldAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFieldAttribute"/> class.
        /// </summary>
        /// <param name="store">Store the original field value in the index. this is useful for short texts
        /// like a document's title which should be displayed with the results. The
        /// value is stored in its original form, i.e. no analyzer is used before it is
        /// stored.</param>
        public TextFieldAttribute(bool store = false)
        {
            Store = store;
        }
    }
}