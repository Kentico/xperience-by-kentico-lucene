using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CMS.DataEngine;
using CMS.DataEngine.Query;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Admin;

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
        properties.PathRows = indexToDisplay?.IncludedPaths.Select(GetPath) ?? new Row[0];
        properties.PathColumns = GetPathColumns();

        properties.PropertyColumns = GetPropertyColumns();

        return Task.FromResult(properties);
    }

    private Row GetPath(IncludedPath attribute) => new()
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
                Caption = LocalizationService.GetString("integrations.lucene.content.columns.stored"),
                ContentType = ColumnContentType.Component
            },
            new Column
            {
                Caption = LocalizationService.GetString("integrations.lucene.content.columns.fieldType"),
                ContentType = ColumnContentType.Component
            },
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



    private static Color GetContentTypeColor(IncludedPath attribute)
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


    private string GetContentTypeLabel(IncludedPath attribute)
    {
        if (attribute.ContentTypes == null || !attribute.ContentTypes.Any())
        {
            var types = DataClassInfoProviderBase<DataClassInfoProvider>.GetClasses().WithObjectType("cms.documenttype");

            //int allTypes = DocumentTypeHelper.GetDocumentTypeClasses()
            //    .GetCount();
            //return string.Format(LocalizationService.GetString("integrations.lucene.content.alltypes"), allTypes);
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
        /// </summary>
        public string? Identifier { get; set; }
    }
}
