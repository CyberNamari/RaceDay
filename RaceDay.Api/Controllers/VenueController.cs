using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/venues")]
[Produces("application/json")]
public class VenuesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public VenuesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists all venues. Open to everyone.</summary>
    /// <response code="200">Returns every venue, ordered by city.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VenueResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenues()
    {
        var venues = await _db.Venues.AsNoTracking()
            .OrderBy(v => v.City).ThenBy(v => v.VenueName)
            .ToListAsync();

        return Ok(venues.Select(VenueResponse.From));
    }

    /// <summary>Creates a venue that events can be held at. Organisers only.</summary>
    /// <remarks>Expects venueName, address, city, province and optional latitude/longitude.</remarks>
    /// <response code="201">Venue created.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost]
    [ProducesResponseType(typeof(VenueResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateVenue(CreateVenueRequest request)
    {
        var venue = new Venue
        {
            VenueName = request.VenueName.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, VenueResponse.From(venue));
    }
}