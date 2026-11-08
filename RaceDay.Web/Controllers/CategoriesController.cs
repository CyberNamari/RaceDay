using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public class CategoriesController : BaseController
{
    private readonly ApiClient _api;
    public CategoriesController(ApiClient api) => _api = api;

    private async Task<EventDetailDto?> LoadOwnEvent(int eventId)
    {
        var me = HttpContext.Session.GetCurrentUser()!;
        var ev = await _api.GetAsync<EventDetailDto>($"api/events/{eventId}");
        if (!ev.Success || ev.Data is null || ev.Data.OrganiserId != me.UserId) return null;
        return ev.Data;
    }

    private IActionResult NotYours()
    {
        TempData["Error"] = "We couldn't find that event among your events.";
        return RedirectToAction("Index", "ManageEvents");
    }

    private static CategoriesPageViewModel Page(EventDetailDto ev, CategoryFormViewModel? form = null) => new()
    {
        EventId = ev.EventId,
        EventName = ev.EventName,
        Categories = ev.Categories,
        Form = form ?? new CategoryFormViewModel { EventId = ev.EventId }
    };

    // id = eventId
    public async Task<IActionResult> Index(int id)
    {
        var ev = await LoadOwnEvent(id);
        if (ev is null) return NotYours();
        return View(Page(ev));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int id, CategoryFormViewModel model)
    {
        var ev = await LoadOwnEvent(id);
        if (ev is null) return NotYours();

        if (!ModelState.IsValid) return View("Index", Page(ev, model));

        var r = await _api.PostAsync<CategoryDto>($"api/events/{id}/categories", new
        {
            categoryName = model.CategoryName,
            distanceKm = model.DistanceKm,
            entryFee = model.EntryFee,
            maxParticipants = model.MaxParticipants
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't add the category.");
            return View("Index", Page(ev, model));
        }

        TempData["Success"] = $"Category '{model.CategoryName}' added.";
        return RedirectToAction(nameof(Index), new { id });
    }

    // id = categoryId
    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var ev = await LoadOwnEvent(eventId);
        var c = ev?.Categories.FirstOrDefault(x => x.CategoryId == id);
        if (ev is null || c is null) return NotYours();

        ViewBag.EventName = ev.EventName;
        return View(new CategoryFormViewModel
        {
            CategoryId = c.CategoryId,
            EventId = eventId,
            CategoryName = c.CategoryName,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var r = await _api.PutAsync<CategoryDto>($"api/categories/{id}", new
        {
            categoryName = model.CategoryName,
            distanceKm = model.DistanceKm,
            entryFee = model.EntryFee,
            maxParticipants = model.MaxParticipants
        });
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (!r.Success)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't update the category.");
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { id = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var r = await _api.DeleteAsync($"api/categories/{id}");
        var expired = SessionExpired(r);
        if (expired != null) return expired;

        if (r.Success) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = r.Error;   // e.g. "already has registrations"
        return RedirectToAction(nameof(Index), new { id = eventId });
    }
}