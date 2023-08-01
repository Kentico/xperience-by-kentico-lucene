using Kentico.Forms.Web.Mvc;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Components.FormSections.TitledSection
{
    public class TitledSectionProperties : IFormSectionProperties
    {
        [RichTextEditorComponent(Label = "Title")]
        public string Title { get; set; }
    }
}
