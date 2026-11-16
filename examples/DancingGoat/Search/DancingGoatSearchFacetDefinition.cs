#nullable enable

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Search;

/// <summary>
/// Describes a taxonomy facet: how it is indexed (<see cref="Dimension"/>, <see cref="GetTags"/>)
/// and how it is queried and displayed (<see cref="QueryParameter"/>, <see cref="Label"/>, <see cref="TaxonomyName"/>).
/// </summary>
/// <param name="Dimension">The Lucene facet dimension.</param>
/// <param name="QueryParameter">The query string parameter holding the selected tag identifier.</param>
/// <param name="Label">The resource string used as the facet group heading.</param>
/// <param name="TaxonomyName">The taxonomy used to resolve tag titles.</param>
/// <param name="GetTags">
/// Returns the tags for the facet from the indexed source (an <see cref="ArticlePage"/> or an <see cref="IProductFields"/> product),
/// or <see langword="null"/> when the source has no such field.
/// </param>
/// <param name="IsOpenByDefault">Indicates whether the facet group is expanded when nothing is selected in it.</param>
public sealed record DancingGoatSearchFacetDefinition(
    string Dimension,
    string QueryParameter,
    string Label,
    string TaxonomyName,
    Func<object, IEnumerable<TagReference>?> GetTags,
    bool IsOpenByDefault = false);
