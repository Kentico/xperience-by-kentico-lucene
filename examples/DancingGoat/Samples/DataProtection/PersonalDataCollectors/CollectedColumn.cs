namespace Samples.DancingGoat
{
    /// <summary>
    /// Represents a column collected by personal data collector.
    /// </summary>
    internal class CollectedColumn
    {
        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        public string Name { get; }


        /// <summary>
        /// Gets the display name of the column.
        /// </summary>
        public string DisplayName { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="CollectedColumn"/> class from given name and display name.
        /// </summary>
        /// <param name="name">Name of the column.</param>
        /// <param name="displayName">Display name of the column.</param>
        public CollectedColumn(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }
    }
}
