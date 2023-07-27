using System.Runtime.CompilerServices;

using CMS;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Services;
using Kentico.Xperience.Lucene.Services.Implementations;

[assembly: AssemblyDiscoverable]

// Allows the Lucene test project to read internal members
[assembly: InternalsVisibleTo("Kentico.Xperience.Lucene.Tests")]

// Modules
[assembly: RegisterModule(typeof(LuceneSearchModule))]
[assembly: RegisterModule(typeof(LuceneAdminModule))]

// UI applications
[assembly: UIApplication(LuceneApplication.IDENTIFIER, typeof(LuceneApplication), "lucene", "{$integrations.lucene.applicationname$}", BaseApplicationCategories.DEVELOPMENT, Icons.Magnifier, TemplateNames.SECTION_LAYOUT)]

// Admin UI pages
[assembly: UIPage(typeof(LuceneApplication), "Indexes", typeof(IndexListing), "{$integrations.lucene.listing$}", TemplateNames.LISTING, UIPageOrder.First)]
[assembly: UIPage(typeof(IndexListing), PageParameterConstants.PARAMETERIZED_SLUG, typeof(ViewIndexSection), "{$integrations.lucene.section.viewindex$}", TemplateNames.SECTION_LAYOUT, UIPageOrder.NoOrder)]
[assembly: UIPage(typeof(ViewIndexSection), "Content", typeof(IndexedContent), "{$integrations.lucene.content$}", "@kentico/xperience-integrations-lucene/IndexedContent", UIPageOrder.First)]
[assembly: UIPage(typeof(IndexedContent), PageParameterConstants.PARAMETERIZED_SLUG, typeof(PathDetail), "{$integrations.lucene.pathdetail$}", "@kentico/xperience-integrations-lucene/PathDetail", UIPageOrder.First)]

// Default service implementations
[assembly: RegisterImplementation(typeof(ILuceneClient), typeof(DefaultLuceneClient), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(ILuceneModelGenerator), typeof(DefaultLuceneModelGenerator), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(ILuceneTaskLogger), typeof(DefaultLuceneTaskLogger), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(ILuceneTaskProcessor), typeof(DefaultLuceneTaskProcessor), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(ILuceneSearchModelToDocumentMapper), typeof(DefaultLuceneSearchModelToDocumentMapper), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]


[assembly: RegisterImplementation(typeof(ILuceneIndexService), typeof(DefaultLuceneIndexService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
