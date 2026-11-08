using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class DashboardRowViewModel
{
    public EventSummaryDto Event { get; set; } = new();
    public int TotalEnrolments { get; set; }
    public int Confirmed { get; set; }
    public int Pending { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardRowViewModel> Rows { get; set; } = new();
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int PendingEnrolments { get; set; }
    public string? Error { get; set; }
}

public class ManageEventRowViewModel
{
    public EventSummaryDto Event { get; set; } = new();
    public string Distances { get; set; } = "None yet";
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required, StringLength(150), Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Required(ErrorMessage = "Please choose a date."), Display(Name = "Date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please choose a start time."), Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Please add a description.")]
    public string? Description { get; set; }

    // Only used when creating (the API can't change a venue later)
    [Display(Name = "Venue")]
    public int VenueId { get; set; }

    // Only used when editing
    public string Status { get; set; } = "Scheduled";
    public string VenueName { get; set; } = "";

    public List<VenueDto> Venues { get; set; } = new();
}

public class VenueFormViewModel
{
    [Required, StringLength(100), Display(Name = "Venue name")]
    public string VenueName { get; set; } = "";

    [Required, StringLength(255)]
    public string Address { get; set; } = "";

    [Required, StringLength(100)]
    public string City { get; set; } = "";

    [Required, StringLength(100)]
    public string Province { get; set; } = "";

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }
}