using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Components.FormSections.TitledSection
{
    public class TitledSectionProperties : IFormSectionProperties
    {
        [RichTextEditorComponent(Label = "{$dancinggoat.titledsection.title.label$}")]
        public string Title { get; set; }
    }
}
