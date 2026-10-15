using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private Task<Event?> LoadDetail(int eventId) =>
        _db.Events.AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == eventId);

    /// <summary>Lists events. Open to everyone.</summary>
    /// <remarks>Optional filters: city, eventType (Run, Walk or Cycle) and date (yyyy-MM-dd). Results are ordered by date.</remarks>
    /// <response code="200">Returns the matching events.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] string? city, [FromQuery] string? eventType, [FromQuery] DateOnly? date)
    {
        var query = _db.Events.AsNoTracking().Include(e => e.Venue).AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityFilter = city.Trim();
            query = query.Where(e => e.Venue.City == cityFilter);
        }
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var typeFilter = eventType.Trim();
            query = query.Where(e => e.EventType == typeFilter);
        }
        if (date.HasValue)
        {
            var dateFilter = date.Value;
            query = query.Where(e => e.EventDate == dateFilter);
        }

        var events = await query.OrderBy(e => e.EventDate).ThenBy(e => e.StartTime).ToListAsync();
        return Ok(events.Select(EventSummaryResponse.From));
    }

    /// <summary>Gets one event with its venue and categories. Open to everyone.</summary>
    /// <response code="200">Returns the event details.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("{eventId:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int eventId)
    {
        var ev = await LoadDetail(eventId);
        if (ev is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(EventDetailResponse.From(ev));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>Expects eventName, eventType (Run, Walk or Cycle), eventDate, startTime, description and the venueId of an existing venue.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">A field is missing or invalid, or the venue does not exist.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(CreateEventRequest request)
    {
        if (!await _db.Venues.AnyAsync(v => v.VenueID == request.VenueId))
        {
            return BadRequest(new { message = "The selected venue does not exist." });
        }

        var ev = new Event
        {
            OrganiserID = User.GetUserId(),
            VenueID = request.VenueId,
            EventName = request.EventName.Trim(),
            EventType = request.EventType,
            EventDate = request.EventDate!.Value,
            StartTime = request.StartTime!.Value,
            Description = request.Description,
            Status = "Scheduled"
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await LoadDetail(ev.EventID);
        return CreatedAtAction(nameof(GetEvent), new { eventId = ev.EventID }, EventDetailResponse.From(created!));
    }

    /// <summary>Updates an event. Only the Organiser who owns it can do this.</summary>
    /// <remarks>Expects eventName, eventType, eventDate, startTime, description and status (Scheduled, Cancelled or Completed).</remarks>
    /// <response code="200">Returns the updated event.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPut("{eventId:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int eventId, UpdateEventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change your own events." });
        }

        ev.EventName = request.EventName.Trim();
        ev.EventType = request.EventType;
        ev.EventDate = request.EventDate!.Value;
        ev.StartTime = request.StartTime!.Value;
        ev.Description = request.Description;
        ev.Status = request.Status;
        await _db.SaveChangesAsync();

        var updated = await LoadDetail(eventId);
        return Ok(EventDetailResponse.From(updated!));
    }

    /// <summary>Deletes an event and its categories. Only the owning Organiser can do this.</summary>
    /// <remarks>An event that already has registrations cannot be deleted; set its status to Cancelled instead.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">The event already has registrations.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpDelete("{eventId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });
        }
        if (await _db.Registrations.AnyAsync(r => r.Category.EventID == eventId))
        {
            return Conflict(new { message = "This event already has registrations. Set its status to Cancelled instead." });
        }

        var categories = await _db.Categories.Where(c => c.EventID == eventId).ToListAsync();
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}