using System.Reflection;

using CMS.DataEngine.Query;
using CMS.DocumentEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// An admin UI page which displays the indexed paths and properties of an Lucene index.
    /// </summary>
    internal class IndexedContent : Page<IndexedContentPageClientProperties>
    {
        private readonly IPageUrlGenerator pageUrlGenerator;
        private LuceneIndex? indexToDisplay;


        /// <summary>
        /// The internal <see cref="LuceneIndex.Identifier"/> of the index.
        /// </summary>
        [PageParameter(typeof(IntPageModelBinder))]
        public int IndexIdentifier
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedContent"/> class.
        /// </summary>
        public IndexedContent(IPageUrlGenerator pageUrlGenerator) => this.pageUrlGenerator = pageUrlGenerator;


        /// <summary>
        /// A page command which displays details of a particular indexed path.
        /// </summary>
        /// <param name="args">The arguments emitted by the template.</param>
        [PageCommand]
        public Task<INavigateResponse> ShowPathDetail(PathDetailArguments args) => Task.FromResult(NavigateTo(pageUrlGenerator.GenerateUrl(typeof(PathDetail), IndexIdentifier.ToString(), args.Identifier)));


        /// <inheritdoc/>
        public override Task<IndexedContentPageClientProperties> ConfigureTemplateProperties(IndexedContentPageClientProperties properties)
        {
            properties.PathRows = indexToDisplay?.IncludedPaths.Select(attr => GetPath(attr)) ?? new Row[0];
            properties.PathColumns = GetPathColumns();

            var searchModelProperties = indexToDisplay?.LuceneSearchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            properties.PropertyRows = searchModelProperties?.Select(prop => GetProperty(prop)) ?? new Row[0];
            properties.PropertyColumns = GetPropertyColumns();

            return Task.FromResult(properties);
        }


        /// <inheritdoc/>
        public override Task<PageValidationResult> ValidatePage()
        {
            indexToDisplay = IndexStore.Instance.GetIndex(IndexIdentifier);
            if (indexToDisplay == null)
            {
                return Task.FromResult(new PageValidationResult
                {
                    IsValid = false,
                    ErrorMessageKey = "integrations.lucene.error.noindex",
                    ErrorMessageParams = new object[] { IndexIdentifier }
                });
            }

            return base.ValidatePage();
        }


        private Row GetPath(IncludedPathAttribute attribute) => new()
        {
            Identifier = attribute.Identifier,
            Cells = new Cell[] {
                    new StringCell
                    {
                        Value = attribute.AliasPath
                    },
                    new NamedComponentCell
                    {
                        Name = NamedComponentCellComponentNames.TAG_COMPONENT,
                        ComponentProps = new TagTableCellComponentProps
                        {
                            Label = GetContentTypeLabel(attribute),
                            Color = GetContentTypeColor(attribute),
                            TooltipText = string.Empty
                        }
                    }
                }
        };


        private Column[] GetPathColumns() => new Column[] {
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.path"),
                    ContentType = ColumnContentType.Text
                },
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.pagetypes"),
                    ContentType = ColumnContentType.Component
                }
            };


        private static Row GetProperty(PropertyInfo property)
        {
            //var isSearchable = Attribute.IsDefined(property, typeof(SearchableAttribute));
            //var isRetrievable = Attribute.IsDefined(property, typeof(RetrievableAttribute));
            //var isFacetable = Attribute.IsDefined(property, typeof(FacetableAttribute));
            // TODO: read from attributes
            bool isSearchable = false;
            bool isRetrievable = false;
            bool hasSources = Attribute.IsDefined(property, typeof(SourceAttribute));
            bool hasUrls = Attribute.IsDefined(property, typeof(MediaUrlsAttribute));
            return new Row
            {
                Cells = new Cell[] {
                    new StringCell
                    {
                        Value = property.Name
                    },
                    new NamedComponentCell
                    {
                        Name = NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                        ComponentProps = new SimpleStatusNamedComponentCellProps
                        {
                            IconName = GetIconName(isSearchable),
                            IconColor = GetIconColor(isSearchable)
                        }
                    },
                    new NamedComponentCell
                    {
                        Name = NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                        ComponentProps = new SimpleStatusNamedComponentCellProps
                        {
                            IconName = GetIconName(isRetrievable),
                            IconColor = GetIconColor(isRetrievable)
                        }
                    },
                    //new NamedComponentCell
                    //{
                    //    Name = NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                    //    ComponentProps = new SimpleStatusNamedComponentCellProps
                    //    {
                    //        IconName = GetIconName(isFacetable),
                    //        IconColor = GetIconColor(isFacetable)
                    //    }
                    //},
                    new NamedComponentCell
                    {
                        Name = NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                        ComponentProps = new SimpleStatusNamedComponentCellProps
                        {
                            IconName = GetIconName(hasSources),
                            IconColor = GetIconColor(hasSources)
                        }
                    },
                    new NamedComponentCell
                    {
                        Name = NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                        ComponentProps = new SimpleStatusNamedComponentCellProps
                        {
                            IconName = GetIconName(hasUrls),
                            IconColor = GetIconColor(hasUrls)
                        }
                    }
                }
            };
        }


        private Column[] GetPropertyColumns() => new Column[] {
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.property"),
                    ContentType = ColumnContentType.Text
                },
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.searchable"),
                    ContentType = ColumnContentType.Component
                },
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.retrievable"),
                    ContentType = ColumnContentType.Component
                },
                //new Column
                //{
                //    Caption = LocalizationService.GetString("integrations.lucene.content.columns.facetable"),
                //    ContentType = ColumnContentType.Component
                //},
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.source"),
                    ContentType = ColumnContentType.Component
                },
                new Column
                {
                    Caption = LocalizationService.GetString("integrations.lucene.content.columns.url"),
                    ContentType = ColumnContentType.Component
                }
            };


        private static Color GetIconColor(bool status) => status ? Color.SuccessIcon : Color.IconLowEmphasis;


        private static string GetIconName(bool status) => status ? Icons.Check : Icons.Minus;


        private static Color GetContentTypeColor(IncludedPathAttribute attribute)
        {
            if (attribute.ContentTypes == null || !attribute.ContentTypes.Any())
            {
                return Color.BackgroundTagGrey;
            }
            else if (attribute.ContentTypes.Length == 1)
            {
                return Color.BackgroundTagUltramarineBlue;
            }

            return Color.BackgroundTagDefault;
        }


        private string GetContentTypeLabel(IncludedPathAttribute attribute)
        {
            if (attribute.ContentTypes == null || !attribute.ContentTypes.Any())
            {
                int allTypes = DocumentTypeHelper.GetDocumentTypeClasses()
                    .OnSite(SiteService.CurrentSite?.SiteID)
                    .GetCount();
                return string.Format(LocalizationService.GetString("integrations.lucene.content.alltypes"), allTypes);
            }
            else if (attribute.ContentTypes.Length == 1)
            {
                return LocalizationService.GetString("integrations.lucene.content.singletype");
            }

            return string.Format(LocalizationService.GetString("integrations.lucene.content.multipletypes"), attribute.ContentTypes.Length);
        }


        /// <summary>
        /// The arguments emitted by the template for use in the <see cref="ShowPathDetail"/> command.
        /// </summary>
        internal class PathDetailArguments
        {
            /// <summary>
            /// The identifier of the row clicked, which corresponds with the internal
            /// <see cref="IncludedPathAttribute.Identifier"/>.
            /// </summary>
            public string? Identifier { get; set; }
        }
    }
}
