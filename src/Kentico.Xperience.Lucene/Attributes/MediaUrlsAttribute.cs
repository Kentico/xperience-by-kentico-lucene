using System;

namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute used for fields with the "Media files" data type. Converts
    /// the field value into a list of strings containing their absolute URLs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MediaUrlsAttribute : Attribute
    {
    }
}