using System;
using System.Collections.Generic;
using System.Linq;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Decides which navigation menu item is highlighted for the current request.
    /// </summary>
    /// <remarks>
    /// Kept out of the view and free of request state so both the site menu and any future menu can share it.
    /// </remarks>
    internal static class NavigationActivePathResolver
    {
        /// <summary>
        /// Normalizes a URL into the form the comparison uses: no application-relative prefix and no
        /// trailing slash, so the site root and a language root both become an empty string.
        /// </summary>
        /// <param name="url">URL to normalize. A null value is treated as the site root.</param>
        public static string Normalize(string url)
        {
            var path = url ?? "/";

            if (path.StartsWith("~", StringComparison.Ordinal))
            {
                path = path[1..];
            }

            return path.TrimEnd('/');
        }


        /// <summary>
        /// Returns the path of the menu item to highlight, or <c>null</c> when no item matches the current request.
        /// </summary>
        /// <param name="paths">Normalized paths of the menu items.</param>
        /// <param name="currentPath">Normalized path of the current request.</param>
        /// <param name="rootPath">Normalized path of the home page.</param>
        public static string GetActivePath(IEnumerable<string> paths, string currentPath, string rootPath)
        {
            return paths
                .Where(path => currentPath.Equals(path, StringComparison.InvariantCultureIgnoreCase)
                    || (!path.Equals(rootPath, StringComparison.InvariantCultureIgnoreCase)
                        && currentPath.StartsWith($"{path}/", StringComparison.InvariantCultureIgnoreCase)))
                .MaxBy(path => path.Length);
        }
    }
}
