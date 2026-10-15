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
public class RegistrationsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public RegistrationsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private IQueryable<Registration> RegistrationsWithDetails() =>
        _db.Registrations.AsNoTracking()
            .Include(r => r.Participant)
            .Include(r => r.Category).ThenInclude(c => c.Event)
            .Include(r => r.Result);

    /// <summary>Enters the logged-in Participant into an event by selecting a category.</summary>
    /// <remarks>No request body is needed; the participant comes from the session. The system records the link between the participant, the event and the category, and assigns a bib number.</remarks>
    /// <response code="201">Entry recorded. Returns the registration with its bib number.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">The category does not exist.</response>
    /// <response code="409">Already entered this event, the category is full, or the event is not open for entries.</response>
    [Authorize(Roles = RoleNames.Participant)]
    [HttpPost("categories/{categoryId:int}/registrations")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null)
        {
            return NotFound(new { message = "Category not found." });
        }
        if (category.Event.Status != "Scheduled")
        {
            return Conflict(new { message = "This event is not open for entries." });
        }

        var userId = User.GetUserId();
        if (await _db.Registrations.AnyAsync(r => r.ParticipantID == userId && r.Category.EventID == category.EventID))
        {
            return Conflict(new { message = "You have already entered this event." });
        }
        if (category.MaxParticipants.HasValue &&
            await _db.Registrations.CountAsync(r => r.CategoryID == categoryId) >= category.MaxParticipants.Value)
        {
            return Conflict(new { message = "This category is full." });
        }

        var existingBibs = await _db.Registrations
            .Where(r => r.Category.EventID == category.EventID && r.BibNumber != null)
            .Select(r => r.BibNumber!)
            .ToListAsync();
        var nextBib = existingBibs.Select(b => int.TryParse(b, out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;

        var registration = new Registration
        {
            ParticipantID = userId,
            CategoryID = categoryId,
            BibNumber = nextBib.ToString(),
            RegistrationDate = DateTime.Now,
            PaymentStatus = "Pending"
        };

        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();

        var created = await RegistrationsWithDetails().FirstAsync(r => r.RegistrationID == registration.RegistrationID);
        return StatusCode(StatusCodes.Status201Created, RegistrationResponse.From(created));
    }

    /// <summary>Lists the logged-in Participant's own entries, including their result once captured.</summary>
    /// <response code="200">Returns the participant's entries, newest event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [Authorize(Roles = RoleNames.Participant)]
    [HttpGet("registrations/me")]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var userId = User.GetUserId();
        var list = await RegistrationsWithDetails().Where(r => r.ParticipantID == userId).ToListAsync();

        return Ok(list.OrderByDescending(r => r.Category.Event.EventDate).Select(r => RegistrationResponse.From(r)));
    }

    /// <summary>Lists every entry for an event. Only the owning Organiser can do this.</summary>
    /// <response code="200">Returns the event's entries, grouped by category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpGet("events/{eventId:int}/registrations")]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventRegistrations(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view entries for your own events." });
        }

        var list = await RegistrationsWithDetails().Where(r => r.Category.EventID == eventId).ToListAsync();

        return Ok(list.OrderBy(r => r.Category.CategoryName).ThenBy(r => r.RegistrationDate).Select(r => RegistrationResponse.From(r)));
    }

    /// <summary>Cancels the logged-in Participant's own entry.</summary>
    /// <remarks>An entry that already has a result cannot be cancelled.</remarks>
    /// <response code="204">Entry cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser, or the entry belongs to someone else.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">A result has already been captured for this entry.</response>
    [Authorize(Roles = RoleNames.Participant)]
    [HttpDelete("registrations/{registrationId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelRegistration(int registrationId)
    {
        var registration = await _db.Registrations.Include(r => r.Result)
            .FirstOrDefaultAsync(r => r.RegistrationID == registrationId);
        if (registration is null)
        {
            return NotFound(new { message = "Entry not found." });
        }
        if (registration.ParticipantID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only cancel your own entries." });
        }
        if (registration.Result is not null)
        {
            return Conflict(new { message = "A result has already been captured for this entry." });
        }

        _db.Registrations.Remove(registration);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
