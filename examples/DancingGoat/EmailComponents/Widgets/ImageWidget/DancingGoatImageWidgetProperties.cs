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
        Label = "Image",
        ExplanationText = "Select the image from assets stored in the Content hub.",
        MaximumItems = 1)]
    public IEnumerable<ContentItemReference> Assets { get; set; } = [];


    /// <summary>
    /// The horizontal alignment of the button. <see cref="DancingGoatHorizontalAlignment"/>
    /// </summary>
    [DropDownComponent(
        Label = "Alignment",
        Order = 2,
        ExplanationText = "Allows you to set the width of the image in pixels.",
        Options = $"{nameof(DancingGoatHorizontalAlignment.Left)};Left\r\n{nameof(DancingGoatHorizontalAlignment.Center)};Center\r\n{nameof(DancingGoatHorizontalAlignment.Right)};Right",
        OptionsValueSeparator = ";")]
    public string Alignment { get; set; } = nameof(DancingGoatHorizontalAlignment.Center);


    /// <summary>
    /// The image width.
    /// </summary>
    [NumberInputComponent(
        Label = "Width",
        Order = 3,
        ExplanationText = "Allows you to set the width of the image in pixels.")]
    public int? Width { get; set; }


    /// <summary>
    /// The image width.
    /// </summary>
    [NumberInputComponent(
        Label = "Height",
        Order = 4,
        ExplanationText = "Allows you to set the height of the image in pixels.")]
    public int? Height { get; set; }
}
