using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageRoute(CoffeeSection.CLASS_NAME, typeof(CoffeesController))]
[assembly: RegisterPageRoute(Coffee.CLASS_NAME, typeof(CoffeesController), ActionName = "Detail")]

namespace DancingGoat.Controllers
{
    public class CoffeesController : Controller
    {
        private readonly IPageUrlRetriever pageUrlRetriever;
        private readonly CoffeeRepository coffeeRepository;


        public CoffeesController(IPageUrlRetriever pageUrlRetriever,
            CoffeeRepository coffeeRepository)
        {
            this.pageUrlRetriever = pageUrlRetriever;
            this.coffeeRepository = coffeeRepository;
        }


        public async Task<IActionResult> Index([FromServices] IPageDataContextRetriever pageDataContextRetriever, CancellationToken cancellationToken)
        {
            var section = pageDataContextRetriever.Retrieve<TreeNode>().Page;
            var coffees = await coffeeRepository.Get(section.NodeAliasPath, cancellationToken: cancellationToken);

            var viewModels = coffees.Select(coffee => new CoffeeViewModel
            {
                Name = coffee.DocumentName,
                Description = coffee.CoffeeDescription,
                ImagePath = (coffee.Fields.Image.FirstOrDefault() as Media)?.Fields.File?.Url,
                Url = pageUrlRetriever.Retrieve(coffee).RelativePath
            });

            return View(viewModels);
        }


        public async Task<IActionResult> Detail(CancellationToken cancellationToken)
        {
            var coffee = await coffeeRepository.GetCurrent(3, cancellationToken);

            var media = coffee.Fields.Image.FirstOrDefault() as Media;

            return View(new CoffeeViewModel
            {
                Description = coffee.CoffeeDescription,
                ImagePath = media?.Fields.File?.Url,
                Name = coffee.DocumentName,
                Url = pageUrlRetriever.Retrieve(coffee).RelativePath
            });
        }
    }
}
