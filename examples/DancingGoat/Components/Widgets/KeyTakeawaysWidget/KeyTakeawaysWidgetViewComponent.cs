using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(KeyTakeawaysWidgetViewComponent.IDENTIFIER, typeof(KeyTakeawaysWidgetViewComponent), "{$dancinggoat.keytakeawayswidget.title$}", typeof(KeyTakeawaysWidgetProperties), Description = "{$dancinggoat.keytakeawayswidget.description$}", IconClass = "icon-list-bullets")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View component for Key takeaways widget.
    /// </summary>
    public class KeyTakeawaysWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.General.KeyTakeawaysWidget";


        /// <summary>
        /// Invokes the widget view.
        /// </summary>
        /// <param name="properties">Widget properties.</param>
        public ViewViewComponentResult Invoke(KeyTakeawaysWidgetProperties properties)
        {
            var model = KeyTakeawaysWidgetViewModel.GetViewModel(properties);

            return View("~/Components/Widgets/KeyTakeawaysWidget/_KeyTakeawaysWidget.cshtml", model);
        }
    }
}
