﻿namespace Kentico.Xperience.Lucene.Attributes;

/// <summary>
/// A property attribute which specifies the LuceneIOndex field type.
/// Field that indexes <see cref="float"/> values
/// for efficient range filtering and sorting.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SingleFieldAttribute : BaseFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleFieldAttribute"/> class.
    /// </summary>
    /// <param name="store">Store the original field value in the index. this is useful for short texts
    /// like a document's title which should be displayed with the results. The
    /// value is stored in its original form, i.e. no analyzer is used before it is
    /// stored.</param>
    public SingleFieldAttribute(bool store = false) => Store = store;
}
