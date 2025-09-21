using Kentico.Xperience.Admin.Base;

namespace Kentico.Xperience.Lucene.Admin.Components;

/// <summary>
/// Represents the result of a row action indicating success or failure.
/// </summary>
internal class RowActionSuccessResult : RowActionResult
{
    /// <summary>
    /// Indicates whether the action was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <inheritdoc/>
    public RowActionSuccessResult(bool reload, bool success, bool refatchAll = false) : base(reload, refatchAll)
    => Success = success;
}
