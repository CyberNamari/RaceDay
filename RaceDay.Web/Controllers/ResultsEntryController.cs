using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public class ResultsEntryController : BaseController
{
    private readonly ApiClient _api;
    public ResultsEntryController(ApiClient api) => _api = api;

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
            TempData["Error"] = regs.Error;
            return RedirectToAction("Index", "ManageEvents");
        }

        ViewBag.EventId = id;
        ViewBag.EventName = ev.Data.EventName;
        ViewBag.EventStatus = ev.Data.Status;
        return View(regs.Data ?? new List<RegistrationDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Save(ResultInputModel model)
    {
        if (model.Status == "Finished" && model.FinishTime is null)
        {
            TempData["Error"] = "Please enter a finish time for a finished entry.";
            return RedirectToAction(nameof(Index), new { id = model.EventId });
        }

        var body = new
        {
            finishTime = model.FinishTime,
            overallPosition = model.OverallPosition,
            categoryPosition = model.CategoryPosition,
            status = model.Status
        };

        var r = model.ResultId.HasValue
            ? await _api.PutAsync<ResultDto>($"api/results/{model.ResultId}", body)
            : await _api.PostAsync<ResultDto>($"api/registrations/{model.RegistrationId}/results", body);
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (r.Success) TempData["Success"] = "Result saved.";
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index), new { id = model.EventId });
    }

    // Publishing = marking the event Completed so it is clearly closed.
    // Participants already see saved results on My Results.
    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We couldn't load that event.";
            return RedirectToAction(nameof(Index), new { id });
        }

        var d = ev.Data;
        var r = await _api.PutAsync<EventDetailDto>($"api/events/{id}", new
        {
            eventName = d.EventName,
            eventType = d.EventType,
            eventDate = d.EventDate,
            startTime = d.StartTime,
            description = d.Description,
            status = "Completed"
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (r.Success) TempData["Success"] = "Results published. The event is now marked Completed.";
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index), new { id });
    }
}