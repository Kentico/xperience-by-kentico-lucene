using DancingGoat;
using DancingGoat.Models;
using DancingGoat.PageTemplates;
using DancingGoat.Sections;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

// Widgets
[assembly: RegisterWidget(ComponentIdentifiers.TESTIMONIAL_WIDGET, "{$dancinggoat.testimonialwidget.title$}", typeof(TestimonialWidgetProperties), "~/Components/Widgets/TestimonialWidget/_DancingGoat_LandingPage_TestimonialWidget.cshtml", Description = "{$dancinggoat.testimonialwidget.description$}", IconClass = "icon-right-double-quotation-mark")]
[assembly: RegisterWidget(ComponentIdentifiers.CTA_BUTTON_WIDGET, "{$dancinggoat.ctabuttonwidget.title$}", typeof(CTAButtonWidgetProperties), "~/Components/Widgets/CTAButton/_DancingGoat_General_CTAButtonWidget.cshtml", Description = "{$dancinggoat.ctabuttonwidget.description$}", IconClass = "icon-rectangle-a")]

// Sections
[assembly: RegisterSection(ComponentIdentifiers.SINGLE_COLUMN_SECTION, "{$dancinggoat.singlecolumnsection.title$}", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_SingleColumnSection.cshtml", Description = "{$dancinggoat.singlecolumnsection.description$}", IconClass = "icon-square")]
[assembly: RegisterSection(ComponentIdentifiers.TWO_COLUMN_SECTION, "{$dancinggoat.twocolumnsection.title$}", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_TwoColumnSection.cshtml", Description = "{$dancinggoat.twocolumnsection.description$}", IconClass = "icon-l-cols-2")]
[assembly: RegisterSection(ComponentIdentifiers.THREE_COLUMN_SECTION, "{$dancinggoat.threecolumnsection.title$}", typeof(ThreeColumnSectionProperties), "~/Components/Sections/ThreeColumnSection/_DancingGoat_ThreeColumnSection.cshtml", Description = "{$dancinggoat.threecolumnsection.description$}", IconClass = "icon-l-cols-3")]
[assembly: RegisterSection(ComponentIdentifiers.SECTION_75_25, "{$dancinggoat.section7525.title$}", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_Section_75_25.cshtml", Description = "{$dancinggoat.section7525.description$}", IconClass = "icon-l-cols-70-30")]
[assembly: RegisterSection(ComponentIdentifiers.SECTION_25_75, "{$dancinggoat.section2575.title$}", typeof(ThemeSectionProperties), "~/Components/Sections/_DancingGoat_Section_25_75.cshtml", Description = "{$dancinggoat.section2575.description$}", IconClass = "icon-l-cols-30-70")]

// Page templates
[assembly: RegisterPageTemplate(ComponentIdentifiers.LANDING_PAGE_SINGLE_COLUMN_TEMPLATE, "{$dancinggoat.landingpagesinglecolumntemplate.title$}", propertiesType: typeof(LandingPageSingleColumnProperties), customViewName: "~/PageTemplates/LandingPage/_DancingGoat_LandingPageSingleColumn.cshtml", ContentTypeNames = new string[] { LandingPage.CONTENT_TYPE_NAME }, Description = "{$dancinggoat.landingpagesinglecolumntemplate.description$}", IconClass = "xp-l-header-text")]
[assembly: RegisterPageTemplate(ComponentIdentifiers.ARTICLE_TEMPLATE, "{$dancinggoat.articletemplate.title$}", customViewName: "~/PageTemplates/Article/_Article.cshtml", ContentTypeNames = new string[] { ArticlePage.CONTENT_TYPE_NAME }, Description = "{$dancinggoat.articletemplate.description$}", IconClass = "xp-l-text")]
[assembly: RegisterPageTemplate(ComponentIdentifiers.ARTICLE_WITH_SIDEBAR_TEMPLATE, "{$dancinggoat.articlewithsidebartemplate.title$}", customViewName: "~/PageTemplates/Article/_ArticleWithSidebar.cshtml", ContentTypeNames = new string[] { ArticlePage.CONTENT_TYPE_NAME }, Description = "{$dancinggoat.articlewithsidebartemplate.description$}", IconClass = "xp-l-text-col")]
