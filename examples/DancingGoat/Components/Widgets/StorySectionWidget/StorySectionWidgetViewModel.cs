using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Story section widget.
    /// </summary>
    public class StorySectionWidgetViewModel
    {
        private StorySection storySection;


        /// <summary>
        /// Story title.
        /// </summary>
        public string Title { get; set; }


        /// <summary>
        /// Story text.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Gets the AI provenance origin of the given field of the story section.
        /// </summary>
        /// <param name="fieldName">Code name of the story section field.</param>
        /// <returns>Origin of the field, or <see cref="AiProvenanceOrigin.None"/> when the field is not marked.</returns>
        public AiProvenanceOrigin GetAiProvenanceOrigin(string fieldName) => storySection?.GetAiProvenanceOrigin(fieldName) ?? AiProvenanceOrigin.None;


        /// <summary>
        /// Gets ViewModel for <paramref name="storySection"/>.
        /// </summary>
        public static StorySectionWidgetViewModel GetViewModel(StorySection storySection)
        {
            if (storySection == null)
            {
                return null;
            }

            return new StorySectionWidgetViewModel
            {
                Title = storySection.StorySectionTitle,
                Text = storySection.StorySectionText,
                storySection = storySection
            };
        }
    }
}
