using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? city, string? eventType, DateOnly? date)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) query.Add($"city={Uri.EscapeDataString(city)}");
        if (!string.IsNullOrWhiteSpace(eventType)) query.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (date.HasValue) query.Add($"date={date.Value:yyyy-MM-dd}");
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var events = await _api.GetAsync<List<EventSummaryDto>>(url);
        var venues = await _api.GetAsync<List<VenueDto>>("api/venues");
        var today = DateOnly.FromDateTime(DateTime.Today);

        var vm = new HomeViewModel
        {
            City = city,
            EventType = eventType,
            Date = date,
            Events = (events.Data ?? new())
                .Where(e => e.Status == "Scheduled" && e.EventDate >= today)
                .ToList(),
            Cities = (venues.Data ?? new()).Select(v => v.City).Distinct().OrderBy(c => c).ToList(),
            Error = events.Success ? null : events.Error
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}