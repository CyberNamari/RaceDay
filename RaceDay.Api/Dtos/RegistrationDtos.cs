using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegistrationResponse
{
    public int RegistrationId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public ResultResponse? Result { get; set; }

    public static RegistrationResponse From(Registration r) => new()
    {
        RegistrationId = r.RegistrationID,
        ParticipantId = r.ParticipantID,
        ParticipantName = r.Participant.FullName,
        EventId = r.Category.EventID,
        EventName = r.Category.Event.EventName,
        EventDate = r.Category.Event.EventDate,
        CategoryId = r.CategoryID,
        CategoryName = r.Category.CategoryName,
        BibNumber = r.BibNumber,
        RegistrationDate = r.RegistrationDate,
        PaymentStatus = r.PaymentStatus,
        Result = r.Result is null ? null : ResultResponse.From(r.Result, r)
    };
}