using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : BaseController
{
    private readonly ApiClient _api;
    public EventsController(ApiClient api) => _api = api;

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We couldn't load that event.";
            return RedirectToAction("Index", "Home");
        }

        WeatherDto? weather = null;
        if (HttpContext.Session.GetCurrentUser() != null)
        {
            var w = await _api.GetAsync<WeatherDto>($"api/events/{id}/weather");
            var expired = SessionExpired(w);
            if (expired != null) return expired;
            if (w.Success) weather = w.Data;
        }

        return View(new EventDetailViewModel { Event = ev.Data, Weather = weather });
    }
}