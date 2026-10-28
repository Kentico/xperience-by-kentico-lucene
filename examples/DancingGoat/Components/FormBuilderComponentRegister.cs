using DancingGoat.Components.FormSections.TitledSection;

using Kentico.Forms.Web.Mvc;

[assembly: RegisterFormSection("DancingGoat.TitledSection", "{$dancinggoat.titledsection.title$}", "~/Components/FormSections/TitledSection/_TitledSection.cshtml", Description = "{$dancinggoat.titledsection.description$}", IconClass = "icon-rectangle-a", PropertiesType = typeof(TitledSectionProperties))]
