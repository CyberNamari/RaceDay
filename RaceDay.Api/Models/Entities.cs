namespace RaceDay.Api.Models;

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class AppUser
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}

public class Venue
{
    public int VenueID { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public int VenueID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Scheduled";

    public AppUser Organiser { get; set; } = null!;
    public Venue Venue { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? DistanceKM { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}

public class Registration
{
    public int RegistrationID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public string? BibNumber { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string PaymentStatus { get; set; } = "Pending";

    public AppUser Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultID { get; set; }
    public int RegistrationID { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished";

    public Registration Registration { get; set; } = null!;
}