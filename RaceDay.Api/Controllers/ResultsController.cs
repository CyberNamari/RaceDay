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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private static string? ValidateResult(ResultRequest request) =>
        request.Status == "Finished" && request.FinishTime is null
            ? "A finish time is required when the status is 'Finished'."
            : null;

    /// <summary>Captures the finish time and positions for a Participant's entry. Only the owning Organiser can do this.</summary>
    /// <remarks>Expects finishTime (HH:mm:ss), overallPosition, categoryPosition and status (Finished, DNF or DQ). A finish time is required when the status is Finished.</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">A field is invalid, or the finish time is missing for a finished entry.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">A result has already been captured for this entry.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost("registrations/{registrationId:int}/results")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int registrationId, ResultRequest request)
    {
        var registration = await _db.Registrations
            .Include(r => r.Participant)
            .Include(r => r.Category).ThenInclude(c => c.Event)
            .Include(r => r.Result)
            .FirstOrDefaultAsync(r => r.RegistrationID == registrationId);
        if (registration is null)
        {
            return NotFound(new { message = "Entry not found." });
        }
        if (registration.Category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only capture results for your own events." });
        }
        if (registration.Result is not null)
        {
            return Conflict(new { message = "A result has already been captured for this entry." });
        }

        var error = ValidateResult(request);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var result = new Result
        {
            RegistrationID = registrationId,
            FinishTime = request.FinishTime,
            OverallPosition = request.OverallPosition,
            CategoryPosition = request.CategoryPosition,
            Status = request.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ResultResponse.From(result, registration));
    }

    /// <summary>Corrects a captured result. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Returns the updated result.</response>
    /// <response code="400">A field is invalid, or the finish time is missing for a finished entry.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or not the owning Organiser.</response>
    /// <response code="404">The result does not exist.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPut("results/{resultId:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int resultId, ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Registration).ThenInclude(g => g.Participant)
            .Include(r => r.Registration).ThenInclude(g => g.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == resultId);
        if (result is null)
        {
            return NotFound(new { message = "Result not found." });
        }
        if (result.Registration.Category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change results for your own events." });
        }

        var error = ValidateResult(request);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        result.FinishTime = request.FinishTime;
        result.OverallPosition = request.OverallPosition;
        result.CategoryPosition = request.CategoryPosition;
        result.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(ResultResponse.From(result));
    }

    /// <summary>Gets a single result. Open to everyone.</summary>
    /// <response code="200">Returns the result.</response>
    /// <response code="404">The result does not exist.</response>
    [HttpGet("results/{resultId:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(int resultId)
    {
        var result = await _db.Results.AsNoTracking()
            .Include(r => r.Registration).ThenInclude(g => g.Participant)
            .Include(r => r.Registration).ThenInclude(g => g.Category)
            .FirstOrDefaultAsync(r => r.ResultID == resultId);
        if (result is null)
        {
            return NotFound(new { message = "Result not found." });
        }

        return Ok(ResultResponse.From(result));
    }

    /// <summary>Gets the leaderboard for a category, ordered by category position. Open to everyone.</summary>
    /// <response code="200">Returns the category's results, best position first.</response>
    /// <response code="404">The category does not exist.</response>
    [HttpGet("categories/{categoryId:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryResults(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryId))
        {
            return NotFound(new { message = "Category not found." });
        }

        var results = await _db.Results.AsNoTracking()
            .Include(r => r.Registration).ThenInclude(g => g.Participant)
            .Include(r => r.Registration).ThenInclude(g => g.Category)
            .Where(r => r.Registration.CategoryID == categoryId)
            .ToListAsync();

        return Ok(results
            .OrderBy(r => r.CategoryPosition ?? int.MaxValue)
            .ThenBy(r => r.FinishTime ?? TimeOnly.MaxValue)
            .Select(r => ResultResponse.From(r)));
    }
}