using System;
using System.Collections.Generic;

using CMS.ContentEngine;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Key takeaways widget.
    /// </summary>
    public class KeyTakeawaysWidgetViewModel
    {
        private KeyTakeawaysWidgetProperties properties;


        /// <summary>
        /// Heading of the takeaways. Empty when the default heading should be used.
        /// </summary>
        public string Heading { get; set; }


        /// <summary>
        /// Takeaways, one per line of the widget text.
        /// </summary>
        public IReadOnlyList<string> Takeaways { get; set; } = [];


        /// <summary>
        /// Gets the AI provenance origin of the given widget property.
        /// </summary>
        /// <param name="propertyName">Name of the widget property.</param>
        /// <returns>Origin of the property, or <see cref="AiProvenanceOrigin.None"/> when the property is not marked.</returns>
        public AiProvenanceOrigin GetAiProvenanceOrigin(string propertyName) => properties?.GetAiProvenanceOrigin(propertyName) ?? AiProvenanceOrigin.None;


        /// <summary>
        /// Gets ViewModel for <paramref name="properties"/>.
        /// </summary>
        public static KeyTakeawaysWidgetViewModel GetViewModel(KeyTakeawaysWidgetProperties properties)
        {
            var takeaways = (properties.Text ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new KeyTakeawaysWidgetViewModel
            {
                Heading = properties.Heading,
                Takeaways = takeaways,
                properties = properties
            };
        }
    }
}
