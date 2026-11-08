using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public class DashboardController : BaseController
{
    private readonly ApiClient _api;
    public DashboardController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var me = HttpContext.Session.GetCurrentUser()!;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var all = await _api.GetAsync<List<EventSummaryDto>>("api/events");
        if (!all.Success || all.Data is null)
            return View(new DashboardViewModel { Error = all.Error });

        var mine = all.Data
            .Where(e => e.OrganiserId == me.UserId)
            .OrderBy(e => e.EventDate < today)
            .ThenBy(e => e.EventDate)
            .ToList();

        var vm = new DashboardViewModel();
        foreach (var e in mine)
        {
            var regs = await _api.GetAsync<List<RegistrationDto>>($"api/events/{e.EventId}/registrations");
            var expired = SessionExpired(regs);
            if (expired != null) return expired;

            var list = regs.Data ?? new List<RegistrationDto>();
            vm.Rows.Add(new DashboardRowViewModel
            {
                Event = e,
                TotalEnrolments = list.Count,
                Confirmed = list.Count(r => r.PaymentStatus == "Confirmed"),
                Pending = list.Count(r => r.PaymentStatus == "Pending")
            });
        }

        vm.TotalEvents = vm.Rows.Count;
        vm.UpcomingEvents = vm.Rows.Count(r => r.Event.Status == "Scheduled" && r.Event.EventDate >= today);
        vm.TotalEnrolments = vm.Rows.Sum(r => r.TotalEnrolments);
        vm.PendingEnrolments = vm.Rows.Sum(r => r.Pending);
        return View(vm);
    }
}