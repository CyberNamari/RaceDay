using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists the categories (distance or age) available for an event. Open to everyone.</summary>
    /// <response code="200">Returns the event's categories.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponse.From));
    }

    /// <summary>Adds a category (e.g. 10km, 21km, Under 20, Senior) to an event. Only the owning Organiser can do this.</summary>
    /// <remarks>Expects categoryName, entryFee, and optionally distanceKm and maxParticipants.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only add categories to your own events." });
        }

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKM = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoryResponse.From(category));
    }

    /// <summary>Updates a category. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The category does not exist.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPut("categories/{categoryId:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int categoryId, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null)
        {
            return NotFound(new { message = "Category not found." });
        }
        if (category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change categories on your own events." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKM = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
        await _db.SaveChangesAsync();

        return Ok(CategoryResponse.From(category));
    }

    /// <summary>Deletes a category. Only the Organiser who owns the event can do this.</summary>
    /// <remarks>A category that already has registrations cannot be deleted.</remarks>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The category does not exist.</response>
    /// <response code="409">The category already has registrations.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpDelete("categories/{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null)
        {
            return NotFound(new { message = "Category not found." });
        }
        if (category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete categories on your own events." });
        }
        if (await _db.Registrations.AnyAsync(r => r.CategoryID == categoryId))
        {
            return Conflict(new { message = "This category already has registrations and cannot be deleted." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}