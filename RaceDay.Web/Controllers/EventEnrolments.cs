using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public class EventEnrolmentsController : BaseController
{
    private readonly ApiClient _api;
    public EventEnrolmentsController(ApiClient api) => _api = api;

    // id = eventId
    public async Task<IActionResult> Index(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We couldn't load that event.";
            return RedirectToAction("Index", "ManageEvents");
        }

        var regs = await _api.GetAsync<List<RegistrationDto>>($"api/events/{id}/registrations");
        var expired = SessionExpired(regs);
        if (expired != null) return expired;

        if (!regs.Success)
        {
            TempData["Error"] = regs.Error;   // e.g. not your event
            return RedirectToAction("Index", "ManageEvents");
        }

        ViewBag.EventId = id;
        ViewBag.EventName = ev.Data.EventName;
        return View(regs.Data ?? new List<RegistrationDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(int registrationId, int eventId)
    {
        var r = await _api.PutAsync<RegistrationDto>($"api/registrations/{registrationId}/confirm", new { });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (r.Success) TempData["Success"] = "Enrolment confirmed.";
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index), new { id = eventId });
    }
}