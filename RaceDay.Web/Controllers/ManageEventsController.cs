using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public class ManageEventsController : BaseController
{
    private readonly ApiClient _api;
    public ManageEventsController(ApiClient api) => _api = api;

    private async Task LoadVenues(EventFormViewModel vm)
    {
        var v = await _api.GetAsync<List<VenueDto>>("api/venues");
        vm.Venues = v.Data ?? new List<VenueDto>();
    }

    public async Task<IActionResult> Index()
    {
        var me = HttpContext.Session.GetCurrentUser()!;
        var all = await _api.GetAsync<List<EventSummaryDto>>("api/events");
        if (!all.Success || all.Data is null)
        {
            ViewBag.Error = all.Error;
            return View(new List<ManageEventRowViewModel>());
        }

        var rows = new List<ManageEventRowViewModel>();
        foreach (var e in all.Data.Where(x => x.OrganiserId == me.UserId))
        {
            var detail = await _api.GetAsync<EventDetailDto>($"api/events/{e.EventId}");
            var distances = detail.Data?.Categories
                .Where(c => c.DistanceKm.HasValue)
                .Select(c => c.DistanceKm!.Value.ToString("0.##") + " km")
                .ToList() ?? new List<string>();

            rows.Add(new ManageEventRowViewModel
            {
                Event = e,
                Distances = distances.Count > 0 ? string.Join(", ", distances) : "None yet"
            });
        }

        return View(rows.OrderBy(r => r.Event.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new EventFormViewModel();
        await LoadVenues(vm);
        return View("Form", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (model.VenueId <= 0)
            ModelState.AddModelError(nameof(model.VenueId), "Please choose a venue.");

        if (!ModelState.IsValid)
        {
            await LoadVenues(model);
            return View("Form", model);
        }

        var r = await _api.PostAsync<EventDetailDto>("api/events", new
        {
            eventName = model.EventName,
            eventType = model.EventType,
            eventDate = model.EventDate,
            startTime = model.StartTime,
            description = model.Description,
            venueId = model.VenueId
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't create the event.");
            await LoadVenues(model);
            return View("Form", model);
        }

        TempData["Success"] = "Event created. Next, add its distance categories.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var me = HttpContext.Session.GetCurrentUser()!;
        var ev = await _api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We couldn't load that event.";
            return RedirectToAction(nameof(Index));
        }
        if (ev.Data.OrganiserId != me.UserId)
        {
            TempData["Error"] = "You can only edit your own events.";
            return RedirectToAction(nameof(Index));
        }

        return View("Form", new EventFormViewModel
        {
            EventId = ev.Data.EventId,
            EventName = ev.Data.EventName,
            EventType = ev.Data.EventType,
            EventDate = ev.Data.EventDate,
            StartTime = ev.Data.StartTime,
            Description = ev.Data.Description,
            Status = ev.Data.Status,
            VenueId = ev.Data.Venue.VenueId,
            VenueName = $"{ev.Data.Venue.VenueName}, {ev.Data.Venue.City}"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View("Form", model);

        var r = await _api.PutAsync<EventDetailDto>($"api/events/{id}", new
        {
            eventName = model.EventName,
            eventType = model.EventType,
            eventDate = model.EventDate,
            startTime = model.StartTime,
            description = model.Description,
            status = model.Status
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't update the event.");
            return View("Form", model);
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _api.DeleteAsync($"api/events/{id}");
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (r.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }
}