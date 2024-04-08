using DancingGoat;
using DancingGoat.Models;
using DancingGoat.PageTemplates;
using DancingGoat.Sections;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

// Widgets
[assembly: RegisterWidget(ComponentIdentifiers.TESTIMONIAL_WIDGET, "Testimonial", typeof(TestimonialWidgetProperties), "~/Components/Widgets/TestimonialWidget/_DancingGoat_LandingPage_TestimonialWidget.cshtml", Description = "Displays a quotation with its author.", IconClass = "icon-right-double-quotation-mark")]
[assembly: RegisterWidget(ComponentIdentifiers.CTA_BUTTON_WIDGET, "CTA button", typeof(CTAButtonWidgetProperties), "~/Components/Widgets/CTAButton/_DancingGoat_General_CTAButtonWidget.cshtml", Description = "Call to action button with configurable target page.", IconClass = "icon-rectangle-a")]

// Sections
[assembly: RegisterSection(ComponentIdentifiers.SINGLE_COLUMN_SECTION, "1 column", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_SingleColumnSection.cshtml", Description = "Single-column section with one full-width zone.", IconClass = "icon-square")]
[assembly: RegisterSection(ComponentIdentifiers.TWO_COLUMN_SECTION, "2 columns - 50/50", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_TwoColumnSection.cshtml", Description = "Two-column section with zones layout 50% + 50%.", IconClass = "icon-l-cols-2")]
[assembly: RegisterSection(ComponentIdentifiers.THREE_COLUMN_SECTION, "3 columns - 33/33/33", typeof(ThreeColumnSectionProperties), "~/Components/Sections/ThreeColumnSection/_DancingGoat_ThreeColumnSection.cshtml", Description = "Three-column section with zones layout 33% + 33% + 33%.", IconClass = "icon-l-cols-3")]
[assembly: RegisterSection(ComponentIdentifiers.SECTION_75_25, "2 columns - 75/25", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_Section_75_25.cshtml", Description = "Two-column section with zones layout 75% + 25%.", IconClass = "icon-l-cols-70-30")]
[assembly: RegisterSection(ComponentIdentifiers.SECTION_25_75, "2 columns - 25/75", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_Section_25_75.cshtml", Description = "Two-column section with zones layout 25% + 75%.", IconClass = "icon-l-cols-30-70")]

// Page templates
[assembly: RegisterPageTemplate(ComponentIdentifiers.LANDING_PAGE_SINGLE_COLUMN_TEMPLATE, "Single column landing page", propertiesType: typeof(LandingPageSingleColumnProperties), customViewName: "~/PageTemplates/LandingPage/_DancingGoat_LandingPageSingleColumn.cshtml", ContentTypeNames = new string[] { LandingPage.CONTENT_TYPE_NAME }, Description = "A default single column page template with two sections differentiated by a background color.", IconClass = "xp-l-header-text")]
[assembly: RegisterPageTemplate(ComponentIdentifiers.ARTICLE_TEMPLATE, "Article detail", customViewName: "~/PageTemplates/Article/_Article.cshtml", ContentTypeNames = new string[] { ArticlePage.CONTENT_TYPE_NAME }, Description = "Displays an article detail with related articles underneath.", IconClass = "xp-l-text")]
[assembly: RegisterPageTemplate(ComponentIdentifiers.ARTICLE_WITH_SIDEBAR_TEMPLATE, "Article detail with sidebar", customViewName: "~/PageTemplates/Article/_ArticleWithSidebar.cshtml", ContentTypeNames = new string[] { ArticlePage.CONTENT_TYPE_NAME }, Description = "Displays an article detail with sidebar.", IconClass = "xp-l-text-col")]
