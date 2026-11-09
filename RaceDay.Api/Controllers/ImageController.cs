using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class ImagesController : ControllerBase
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public ImagesController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    private static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image file.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType)) return "Only JPEG, PNG or WebP images are allowed.";
        return null;
    }

    /// <summary>Uploads a banner image for an event. Only the owning Organiser can do this.</summary>
    /// <response code="200">Returns the banner URL.</response>
    /// <response code="400">The file is missing, too large or not an image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [Authorize(Roles = RoleNames.Organiser)]
    [HttpPost("events/{eventId:int}/banner")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile? file, CancellationToken ct)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId, ct);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change your own events." });

        var error = Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        try
        {
            ev.BannerUrl = await _blobs.UploadImageAsync($"events/{eventId}", file!, ct);
            await _db.SaveChangesAsync(ct);
            return Ok(new { bannerUrl = ev.BannerUrl });
        }
        catch (Exception ex) when (ex is RequestFailedException or InvalidOperationException or IOException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is currently unavailable." });
        }
    }

    /// <summary>Uploads a profile picture for the logged-in user.</summary>
    /// <response code="200">Returns the picture URL.</response>
    /// <response code="400">The file is missing, too large or not an image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [Authorize]
    [HttpPost("users/me/picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? file, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId, ct);
        if (user is null) return Unauthorized();

        var error = Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        try
        {
            user.ProfilePictureUrl = await _blobs.UploadImageAsync($"profiles/{userId}", file!, ct);
            await _db.SaveChangesAsync(ct);
            return Ok(new { profilePictureUrl = user.ProfilePictureUrl });
        }
        catch (Exception ex) when (ex is RequestFailedException or InvalidOperationException or IOException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is currently unavailable." });
        }
    }
}