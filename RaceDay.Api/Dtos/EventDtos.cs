using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class CreateEventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Must be "Run", "Walk" or "Cycle".</summary>
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be 'Run', 'Walk' or 'Cycle'.")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Format: yyyy-MM-dd, e.g. 2027-06-13.</summary>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <summary>Format: HH:mm:ss, e.g. 05:30:00.</summary>
    [Required]
    public TimeOnly? StartTime { get; set; }

    public string? Description { get; set; }

    /// <summary>The id of an existing venue (see GET /api/venues).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "A valid venueId is required.")]
    public int VenueId { get; set; }
}

public class UpdateEventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be 'Run', 'Walk' or 'Cycle'.")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required]
    public TimeOnly? StartTime { get; set; }

    public string? Description { get; set; }

    /// <summary>Must be "Scheduled", "Cancelled" or "Completed".</summary>
    [Required, RegularExpression("^(Scheduled|Cancelled|Completed)$", ErrorMessage = "Status must be 'Scheduled', 'Cancelled' or 'Completed'.")]
    public string Status { get; set; } = string.Empty;
}

public class EventSummaryResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;

    public static EventSummaryResponse From(Event e) => new()
    {
        EventId = e.EventID,
        EventName = e.EventName,
        EventType = e.EventType,
        EventDate = e.EventDate,
        StartTime = e.StartTime,
        Status = e.Status,
        OrganiserId = e.OrganiserID,
        VenueName = e.Venue.VenueName,
        City = e.Venue.City,
        Province = e.Venue.Province
    };
}

public class EventDetailResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public VenueResponse Venue { get; set; } = null!;
    public List<CategoryResponse> Categories { get; set; } = new();

    public static EventDetailResponse From(Event e) => new()
    {
        EventId = e.EventID,
        EventName = e.EventName,
        EventType = e.EventType,
        EventDate = e.EventDate,
        StartTime = e.StartTime,
        Description = e.Description,
        Status = e.Status,
        OrganiserId = e.OrganiserID,
        OrganiserName = e.Organiser.FullName,
        Venue = VenueResponse.From(e.Venue),
        Categories = e.Categories.OrderBy(c => c.CategoryName).Select(CategoryResponse.From).ToList()
    };
}