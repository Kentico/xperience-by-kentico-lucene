using System.Collections.Generic;
using System.Linq;

using Kentico.PageBuilder.Web.Mvc;

using DancingGoat.Widgets;

namespace DancingGoat.Sections
{
    /// <summary>
    /// Provides filter methods to restrict the list of allowed widgets for widget zones.
    /// </summary>
    public static class ZoneRestrictions
    {
        /// <summary>
        /// The main content widget zone name.
        /// </summary>
        public const string MAIN_ZONE_NAME = "main";

        /// <summary>
        /// The side panel widget zone name.
        /// </summary>
        public const string SIDE_PANEL_ZONE_NAME = "side-panel";


        /// <summary>
        /// Gets list of widget identifiers allowed for narrow widget zones (25%, 33%, 50%).
        /// </summary>
        public static IEnumerable<string> GetNarrowZoneRestrictions()
        {
            var restrictedWidgets = new List<string> {
                BannerWidgetViewComponent.IDENTIFIER,
                HeroImageWidgetViewComponent.IDENTIFIER,
                ArticlesWidgetViewComponent.IDENTIFIER
            };

            return GetWidgetsIdentifiers()
                    .Where(id => !restrictedWidgets.Contains(id));
        }


        /// <summary>
        /// Gets list of widget identifiers allowed for wide widget zones (75%).
        /// </summary>
        public static IEnumerable<string> GetWideZoneRestrictions()
        {
            var restrictedWidgets = new List<string> {
                CafeCardWidgetViewComponent.IDENTIFIER,
                CardWidgetViewComponent.IDENTIFIER,
            };

            return GetWidgetsIdentifiers()
                    .Where(id => !restrictedWidgets.Contains(id));
        }


        private static IEnumerable<string> GetWidgetsIdentifiers()
        {
            return new ComponentDefinitionProvider<WidgetDefinition>()
                   .GetAll()
                   .Select(definition => definition.Identifier);
        }
    }
}