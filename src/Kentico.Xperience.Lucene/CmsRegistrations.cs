using System.Runtime.CompilerServices;

using CMS;
using CMS.Core;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Admin.Components;
using Kentico.Xperience.Lucene.Services;
using Kentico.Xperience.Lucene.Services.Implementations;

// Allows the Lucene test project to read internal members
[assembly: InternalsVisibleTo("Kentico.Xperience.Lucene.Tests")]

// Modules
[assembly: RegisterModule(typeof(LuceneSearchModule))]
[assembly: RegisterModule(typeof(LuceneAdminModule))]

// UI applications
[assembly: UIApplication(LuceneApplication.IDENTIFIER, typeof(LuceneApplication), "lucene", "Search", BaseApplicationCategories.DEVELOPMENT, Icons.Magnifier, TemplateNames.SECTION_LAYOUT)]

// Admin UI pages
[assembly: UIPage(typeof(LuceneApplication), "Indexes", typeof(IndexListing), "List of registered Lucene indices", TemplateNames.LISTING, UIPageOrder.First)]
[assembly: UIPage(typeof(IndexListing), PageParameterConstants.PARAMETERIZED_SLUG, typeof(EditIndex), "Edit index", TemplateNames.EDIT, UIPageOrder.First)]

[assembly: RegisterFormComponent(nameof(ListComponent), typeof(ListComponent), "Custom List component")]

// Default service implementations
[assembly: RegisterImplementation(typeof(ILuceneClient), typeof(DefaultLuceneClient), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(ILuceneTaskLogger), typeof(DefaultLuceneTaskLogger), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(ILuceneTaskProcessor), typeof(DefaultLuceneTaskProcessor), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(IConfigurationStorageService), typeof(DefaultConfigurationStorageService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]

[assembly: RegisterImplementation(typeof(ILuceneIndexService), typeof(DefaultLuceneIndexService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
