using System;
using System.Collections.Generic;
using System.Linq;

using Kentico.PageBuilder.Web.Mvc;

namespace DancingGoat.Helpers
{
    /// <summary>
    /// Provides filter methods to restrict the list of allowed widgets for editable areas.
    /// </summary>
    public static class AreaRestrictionHelper
    {
        /// <summary>
        /// Gets list of widget identifiers allowed for landing page.
        /// </summary>
        public static string[] GetLandingPageRestrictions()
        {
            var allowedScopes = new[] { "Kentico.", "DancingGoat.General.", "DancingGoat.LandingPage." };

            return GetWidgetsIdentifiers()
                .Where(id => Array.Exists(allowedScopes, scope => id.StartsWith(scope, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }


        /// <summary>
        /// Gets list of widget identifiers allowed for home page.
        /// </summary>
        public static string[] GetHomePageRestrictions()
        {
            var deniedScopes = new[] { "DancingGoat.LandingPage." };

            return GetWidgetsIdentifiers()
                .Where(id => Array.Exists(deniedScopes, scope => !id.StartsWith(scope, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }


        private static IEnumerable<string> GetWidgetsIdentifiers()
        {
            return new ComponentDefinitionProvider<WidgetDefinition>()
                   .GetAll()
                   .Select(definition => definition.Identifier);
        }
    }
}
