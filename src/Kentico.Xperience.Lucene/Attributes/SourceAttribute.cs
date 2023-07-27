using System;

namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute which specifies the column names that are used
    /// as the data source for the property, in order of priority.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SourceAttribute : Attribute
    {
        /// <summary>
        /// A list of columns used to load an Lucene search model's property value
        /// from. Columns are checked for non-null values in the order they appear
        /// in the array.
        /// </summary>
        public string[] Sources
        {
            get;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAttribute"/> class.
        /// </summary>
        /// <param name="sources">A list of columns used to load an Lucene search
        /// model's property value from. Columns are checked for non-null values in
        /// the order they appear in the array.</param>
        public SourceAttribute(string[] sources)
        {
            Sources = sources;
        }
    }
}