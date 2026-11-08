using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<LoginResponse>("api/auth/login",
            new { email = model.Email, password = model.Password });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "Login failed.");
            return View(model);
        }

        HttpContext.Session.SetCurrentUser(new SessionUser
        {
            UserId = result.Data.UserId,
            FullName = result.Data.FullName,
            Role = result.Data.Role
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<object>("api/auth/register", new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = model.PhoneNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}