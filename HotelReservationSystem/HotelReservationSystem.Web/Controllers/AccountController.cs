using HotelReservationSystem.Core.Domain.Entities;
using HotelReservationSystem.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

public sealed class AccountController : Controller
{
    private readonly UserManager<Guest> userManager;
    private readonly SignInManager<Guest> signInManager;

    public AccountController(
        UserManager<Guest> userManager,
        SignInManager<Guest> signInManager
        )
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromForm] RegisterViewModel model, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {

            var user = new Guest(model.Email, model.PhoneNumber);

            IdentityResult? result = await this.userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Guest");
                await signInManager.SignInAsync(user, isPersistent: false);

                return Json(new { redirectUrl = returnUrl });
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("~/Views/Shared/Auth/_RegisterForm.cshtml", model);

    }

    [HttpPost]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromForm] LoginViewModel model, [FromForm] string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            var result = await signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
                return Json(new { redirectUrl = returnUrl });

            ModelState.AddModelError(string.Empty, "Invalid password or email");
        }

        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("~/Views/Shared/Auth/_LoginForm.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied(string returnUrl)
    {
        return View();
    }
}
