using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    public class HttpErrorsController : Controller
    {
        public IActionResult Error(int code)
        {
            if (code == 404)
            {
                return View("NotFound");
            }

            if (code == 400)
            {
                return View("BadRequest");
            }

            if (code == 403)
            {
                return View("AccessDenied");
            }

            return StatusCode(code);
        }
    }
}
