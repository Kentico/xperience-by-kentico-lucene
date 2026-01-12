using System.Collections.Generic;

using CMS.ContentEngine;

using DancingGoat.EmailComponents.Enums;
using DancingGoat.Models;

using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.EmailComponents;

/// <summary>
/// Configurable properties of the <see cref="DancingGoatImageWidget"/>.
/// </summary>
public class DancingGoatImageWidgetProperties : IEmailWidgetProperties
{
    /// <summary>
    /// The image.
    /// </summary>
    [ContentItemSelectorComponent(
        Image.CONTENT_TYPE_NAME,
        Order = 1,
        Label = "{$dancinggoat.imagewidget.image.label$}",
        ExplanationText = "{$dancinggoat.imagewidget.image.explanationtext$}",
        MaximumItems = 1)]
    public IEnumerable<ContentItemReference> Assets { get; set; } = [];


    /// <summary>
    /// The horizontal alignment of the button. <see cref="DancingGoatHorizontalAlignment"/>
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.imagewidget.alignment.label$}",
        Order = 2,
        ExplanationText = "{$dancinggoat.imagewidget.alignment.explanationtext$}",
        Options = $"{nameof(DancingGoatHorizontalAlignment.Left)};{{$dancinggoat.imagewidget.alignment.option.left$}}\r\n{nameof(DancingGoatHorizontalAlignment.Center)};{{$dancinggoat.imagewidget.alignment.option.center$}}\r\n{nameof(DancingGoatHorizontalAlignment.Right)};{{$dancinggoat.imagewidget.alignment.option.right$}}",
        OptionsValueSeparator = ";")]
    public string Alignment { get; set; } = nameof(DancingGoatHorizontalAlignment.Center);


    /// <summary>
    /// The image width.
    /// </summary>
    [NumberInputComponent(
        Label = "{$dancinggoat.imagewidget.width.label$}",
        Order = 3,
        ExplanationText = "{$dancinggoat.imagewidget.width.explanationtext$}")]
    public int? Width { get; set; }


    /// <summary>
    /// The image width.
    /// </summary>
    [NumberInputComponent(
        Label = "{$dancinggoat.imagewidget.height.label$}",
        Order = 4,
        ExplanationText = "{$dancinggoat.imagewidget.height.explanationtext$}")]
    public int? Height { get; set; }
}
