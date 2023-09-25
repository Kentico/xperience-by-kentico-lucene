using DancingGoat.Components.FormSections.TitledSection;

using Kentico.Forms.Web.Mvc;

[assembly: RegisterFormSection("DancingGoat.TitledSection", "Section with title", "~/Components/FormSections/TitledSection/_TitledSection.cshtml", Description = "Single-column section with one zone and an editable title", IconClass = "icon-rectangle-a", PropertiesType = typeof(TitledSectionProperties))]