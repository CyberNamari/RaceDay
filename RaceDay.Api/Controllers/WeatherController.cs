using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/weather")]
[Authorize]
[Produces("application/json")]
public class WeatherController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IWeatherService _weather;

    public WeatherController(RaceDayDbContext db, IWeatherService weather)
    {
        _db = db;
        _weather = weather;
    }

    /// <summary>Gets the current weather at an event's venue, for race-day preparation.</summary>
    /// <remarks>Open to any logged-in user. Temperature is in degrees Celsius and wind speed in km/h.</remarks>
    /// <response code="200">Returns temperature, condition, windSpeed and forecastTime.</response>
    /// <response code="400">The event's venue has no coordinates.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="503">The weather provider could not be reached.</response>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetWeather(int eventId, CancellationToken cancellationToken)
    {
        var location = await _db.Events.AsNoTracking()
            .Where(e => e.EventID == eventId)
            .Select(e => new { e.Venue.Latitude, e.Venue.Longitude })
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (location.Latitude is null || location.Longitude is null)
        {
            return BadRequest(new { message = "This event's venue has no coordinates, so weather is unavailable." });
        }

        var weather = await _weather.GetCurrentWeatherAsync(location.Latitude.Value, location.Longitude.Value, cancellationToken);
        if (weather is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The weather service is currently unavailable." });
        }

        return Ok(new WeatherResponse
        {
            Temperature = weather.Temperature,
            Condition = weather.Condition,
            WindSpeed = weather.WindSpeed,
            ForecastTime = weather.ForecastTime
        });
    }
}