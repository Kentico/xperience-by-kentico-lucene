﻿using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CMS.Helpers;

namespace DancingGoat.Search;

public class WebScraperHtmlSanitizer
{
    public virtual string SanitizeHtmlFragment(string htmlContent)
    {

        var parser = new HtmlParser();
#pragma warning disable CS8625 //  Cannot convert null literal to non-nullable reference type.
        // null is relevant parameter
        var nodes = parser.ParseFragment(htmlContent, null);
#pragma warning restore CS8625 //  Cannot convert null literal to non-nullable reference type.

        // Removes script tags
        foreach (var element in nodes.QuerySelectorAll("script"))
        {
            element.Remove();
        }

        // Removes script tags
        foreach (var element in nodes.QuerySelectorAll("style"))
        {
            element.Remove();
        }

        // Removes elements marked with the default Xperience exclusion attribute
        foreach (var element in nodes.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
        {
            element.Remove();
        }

        // Gets the text content of the body element
        string textContent = string.Join(" ", nodes.Select(n => n.TextContent));

        // Normalizes and trims whitespace characters
        textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
        textContent = textContent.Trim();

        return textContent;
    }

    public virtual string SanitizeHtmlDocument(string htmlContent)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(htmlContent);
        var body = doc.Body;
        if (body != null)
        {

            // Removes script tags
            foreach (var element in body.QuerySelectorAll("script"))
            {
                element.Remove();
            }

            // Removes script tags
            foreach (var element in body.QuerySelectorAll("style"))
            {
                element.Remove();
            }

            // Removes elements marked with the default Xperience exclusion attribute
            foreach (var element in body.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
            {
                element.Remove();
            }

            // Removes header
            foreach (var element in body.QuerySelectorAll("header"))
            {
                element.Remove();
            }

            // Removes footer
            foreach (var element in body.QuerySelectorAll(".footer-wrapper"))
            {
                element.Remove();
            }

            // Gets the text content of the body element
            string textContent = body.TextContent;

            // Normalizes and trims whitespace characters
            textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
            textContent = textContent.Trim();

            return textContent;
        }

        return string.Empty;
    }
}
