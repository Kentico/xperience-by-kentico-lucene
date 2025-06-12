using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using CMS.Core;
using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Membership;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace DancingGoat.Controllers
{
    public class AccountController : Controller
    {
        private readonly IStringLocalizer<SharedResources> localizer;
        private readonly IEventLogService eventLogService;
        private readonly IContentRetriever contentRetriever;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IStringLocalizer<SharedResources> localizer,
            IEventLogService eventLogService,
            IContentRetriever contentRetriever)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.localizer = localizer;
            this.eventLogService = eventLogService;
            this.contentRetriever = contentRetriever;
        }


        // GET: Account/Login
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }


        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var signInResult = SignInResult.Failed;

            try
            {
                signInResult = await signInManager.PasswordSignInAsync(model.UserName, model.Password, model.StaySignedIn, false);
            }
            catch (Exception ex)
            {
                eventLogService.LogException("AccountController", "Login", ex);
            }

            if (signInResult.Succeeded)
            {
                var decodedReturnUrl = WebUtility.UrlDecode(returnUrl);
                if (!string.IsNullOrEmpty(decodedReturnUrl) && Url.IsLocalUrl(decodedReturnUrl))
                {
                    return Redirect(decodedReturnUrl);
                }

                return Redirect(await GetHomeWebPageUrl(cancellationToken));
            }

            ModelState.AddModelError(string.Empty, localizer["Your sign-in attempt was not successful. Please try again."].ToString());

            return View(model);
        }


        // POST: Account/Logout 
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Logout(CancellationToken cancellationToken = default)
        {
            await signInManager.SignOutAsync();
            return Redirect(await GetHomeWebPageUrl(cancellationToken));
        }


        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }


        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                Enabled = true
            };

            var registerResult = new IdentityResult();

            try
            {
                registerResult = await userManager.CreateAsync(member, model.Password);
            }
            catch (Exception ex)
            {
                eventLogService.LogException("AccountController", "Register", ex);
                ModelState.AddModelError(string.Empty, localizer["Your registration was not successful."]);
            }

            if (registerResult.Succeeded)
            {
                var signInResult = await signInManager.PasswordSignInAsync(member, model.Password, true, false);

                if (signInResult.Succeeded)
                {
                    return Redirect(await GetHomeWebPageUrl(cancellationToken));
                }
            }

            foreach (var error in registerResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        private async Task<string> GetHomeWebPageUrl(CancellationToken cancellationToken = default)
        {
            var homePage = (await contentRetriever.RetrievePages<HomePage>(
                RetrievePagesParameters.Default,
                query => query.UrlPathColumns(),
                new RetrievalCacheSettings("UrlPathColumns"),
                cancellationToken
            )).FirstOrDefault();

            return homePage.GetUrl().RelativePath;
        }
    }
}
