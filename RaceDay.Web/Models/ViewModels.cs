using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class HomeViewModel
{
    public List<EventSummaryDto> Events { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public string? City { get; set; }
    public string? EventType { get; set; }
    public DateOnly? Date { get; set; }
    public string? Error { get; set; }
}

public class EventDetailViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public WeatherDto? Weather { get; set; }
}

public class ResultRowViewModel
{
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public TimeOnly? FinishTime { get; set; }
    public string Status { get; set; } = "";
    public int? OverallPosition { get; set; }
    public int? TotalOverall { get; set; }
    public int? CategoryPosition { get; set; }
    public int? TotalCategory { get; set; }
}

public class ProfileViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = "";

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = "";
    public DateTime? MemberSince { get; set; }
    public string? ProfilePictureUrl { get; set; }
}