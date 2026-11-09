using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public class VenuesController : BaseController
{
    private readonly ApiClient _api;
    public VenuesController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Create() => View(new VenueFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(VenueFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var r = await _api.PostAsync<VenueDto>("api/venues", new
        {
            venueName = model.VenueName,
            address = model.Address,
            city = model.City,
            province = model.Province,
            latitude = model.Latitude,
            longitude = model.Longitude
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't add the venue.");
            return View(model);
        }

        TempData["Success"] = $"Venue '{model.VenueName}' added. You can now choose it for an event.";
        return RedirectToAction("Create", "ManageEvents");
    }
}