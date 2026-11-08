namespace RaceDay.Web.Models;

public class VenueDto
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
}

public class EventSummaryDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Status { get; set; } = "";
    public int OrganiserId { get; set; }
    public string VenueName { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
}

public class EventDetailDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";
    public VenueDto Venue { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int RegistrationId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string? BibNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "";
}

public class RegistrationDto
{
    public int RegistrationId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string? BibNumber { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string PaymentStatus { get; set; } = "";
    public ResultDto? Result { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class WeatherDto
{
    public decimal Temperature { get; set; }
    public string Condition { get; set; } = "";
    public decimal WindSpeed { get; set; }
    public string ForecastTime { get; set; } = "";
}