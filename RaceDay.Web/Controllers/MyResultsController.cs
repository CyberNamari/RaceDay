using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Participant")]
public class MyResultsController : BaseController
{
    private readonly ApiClient _api;
    public MyResultsController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var reg = await _api.GetAsync<List<RegistrationDto>>("api/registrations/me");
        var expired = SessionExpired(reg);
        if (expired != null) return expired;

        if (!reg.Success || reg.Data is null)
        {
            ViewBag.Error = reg.Error;
            return View(new List<ResultRowViewModel>());
        }

        var withResults = reg.Data.Where(r => r.Result != null).ToList();

        var categoryTotals = new Dictionary<int, int>();
        var eventTotals = new Dictionary<int, int>();

        foreach (var eventId in withResults.Select(r => r.EventId).Distinct())
        {
            var ev = await _api.GetAsync<EventDetailDto>($"api/events/{eventId}");
            if (!ev.Success || ev.Data is null) continue;

            var eventTotal = 0;
            foreach (var c in ev.Data.Categories)
            {
                var res = await _api.GetAsync<List<ResultDto>>($"api/categories/{c.CategoryId}/results");
                if (!res.Success || res.Data is null) continue;

                var finished = res.Data.Count(x => x.Status == "Finished");
                categoryTotals[c.CategoryId] = finished;
                eventTotal += finished;
            }
            eventTotals[eventId] = eventTotal;
        }

        var rows = withResults.Select(r => new ResultRowViewModel
        {
            EventName = r.EventName,
            EventDate = r.EventDate,
            CategoryName = r.CategoryName,
            FinishTime = r.Result!.FinishTime,
            Status = r.Result.Status,
            OverallPosition = r.Result.OverallPosition,
            TotalOverall = eventTotals.GetValueOrDefault(r.EventId),
            CategoryPosition = r.Result.CategoryPosition,
            TotalCategory = categoryTotals.GetValueOrDefault(r.CategoryId)
        }).OrderByDescending(x => x.EventDate).ToList();

        return View(rows);
    }
}