using System;
using System.Net;

using Microsoft.AspNetCore.Html;

namespace DancingGoat.Helpers
{
    /// <summary>
    /// Formats CMS-authored display-heading text for the design's heading conventions.
    /// The accent period that closes a display heading is appended here rather than in CSS,
    /// so it is added exactly once and only when the author has not already closed the
    /// heading with their own terminal punctuation.
    /// </summary>
    public static class HeadingFormatter
    {
        private const string ACCENT_PERIOD = "<span class=\"accent-period\">.</span>";

        private static readonly char[] AUTHORED_TERMINATORS = ['?', '!', '…'];


        /// <summary>
        /// Formats <paramref name="text"/> for use inside a display heading.
        /// </summary>
        /// <param name="text">Heading text.</param>
        /// <returns>
        /// HTML-encoded text closed by the accent period; the text unchanged when it already
        /// ends with the author's own terminal punctuation; or <see cref="HtmlString.Empty"/>
        /// when <paramref name="text"/> is <see langword="null"/> or empty.
        /// </returns>
        public static IHtmlContent Format(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return HtmlString.Empty;
            }

            text = text.TrimEnd();

            if (text.Length == 0)
            {
                return HtmlString.Empty;
            }

            bool endsWithEllipsis = text.EndsWith("...", StringComparison.Ordinal);

            if (text.EndsWith('.') && !endsWithEllipsis)
            {
                text = text[..^1];
            }
            else if (endsWithEllipsis || Array.IndexOf(AUTHORED_TERMINATORS, text[^1]) >= 0)
            {
                return new HtmlString(WebUtility.HtmlEncode(text));
            }

            return new HtmlString(WebUtility.HtmlEncode(text) + ACCENT_PERIOD);
        }
    }
}
