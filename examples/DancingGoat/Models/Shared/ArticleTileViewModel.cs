using System;
using System.Net;

using Microsoft.AspNetCore.Html;

namespace DancingGoat.Models;

/// <summary>
/// View model for the article tile partial view.
/// </summary>
public record ArticleTileViewModel(string Url, string TeaserUrl, string Title, IHtmlContent Summary, DateTime? PublicationDate, bool IsSecured)
{
    /// <summary>
    /// Maps <see cref="ArticleViewModel"/> to a <see cref="ArticleTileViewModel"/>.
    /// </summary>
    public static ArticleTileViewModel GetViewModel(ArticleViewModel article)
    {
        return new ArticleTileViewModel(
            article.Url,
            article.TeaserUrl,
            article.Title,
            new HtmlString(WebUtility.HtmlEncode(article.Summary)),
            article.PublicationDate,
            article.IsSecured
        );
    }


    /// <summary>
    /// Maps <see cref="RelatedPageViewModel"/> to a <see cref="ArticleTileViewModel"/>.
    /// </summary>
    public static ArticleTileViewModel GetViewModel(RelatedPageViewModel relatedPage)
    {
        return new ArticleTileViewModel(
            relatedPage.Url,
            relatedPage.TeaserUrl,
            relatedPage.Title,
            new HtmlString(relatedPage.Summary),
            relatedPage.PublicationDate,
            IsSecured: false
        );
    }
}
