using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin]
public class ProfileController : BaseController
{
    private readonly ApiClient _api;
    public ProfileController(ApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var r = await _api.GetAsync<UserDto>("api/users/me");
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success || r.Data is null)
        {
            TempData["Error"] = r.Error ?? "We couldn't load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(new ProfileViewModel
        {
            FullName = r.Data.FullName,
            Email = r.Data.Email,
            PhoneNumber = r.Data.PhoneNumber,
            Role = r.Data.Role,
            MemberSince = r.Data.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var me = HttpContext.Session.GetCurrentUser()!;
        model.Role = me.Role;
        if (!ModelState.IsValid) return View(model);

        var r = await _api.PutAsync<UserDto>("api/users/me", new
        {
            fullName = model.FullName,
            email = model.Email,
            phoneNumber = model.PhoneNumber
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't update your profile.");
            return View(model);
        }

        me.FullName = model.FullName.Trim();
        HttpContext.Session.SetCurrentUser(me);

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }
}