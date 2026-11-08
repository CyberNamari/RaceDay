using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Participant")]
public class EnrolmentsController : BaseController
{
    private readonly ApiClient _api;
    public EnrolmentsController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.GetAsync<List<RegistrationDto>>("api/registrations/me");
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success) ViewBag.Error = r.Error;
        return View(r.Data ?? new List<RegistrationDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var r = await _api.PostAsync<RegistrationDto>($"api/categories/{categoryId}/registrations");
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            TempData["Error"] = r.Error;
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        TempData["Success"] = $"You're in! Your bib number is {r.Data?.BibNumber}. Your enrolment is Pending until the organiser confirms it.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int registrationId)
    {
        var r = await _api.DeleteAsync($"api/registrations/{registrationId}");
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (r.Success) TempData["Success"] = "Your enrolment has been cancelled.";
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }
}