using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    /// <summary>Format HH:mm:ss, e.g. 08:45:12. Required when the status is "Finished".</summary>
    public TimeOnly? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? OverallPosition { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryPosition { get; set; }

    /// <summary>"Finished", "DNF" (did not finish) or "DQ" (disqualified).</summary>
    [Required, RegularExpression("^(Finished|DNF|DQ)$", ErrorMessage = "Status must be 'Finished', 'DNF' or 'DQ'.")]
    public string Status { get; set; } = "Finished";
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int RegistrationId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = string.Empty;

    public static ResultResponse From(Result result, Registration reg) => new()
    {
        ResultId = result.ResultID,
        RegistrationId = result.RegistrationID,
        ParticipantName = reg.Participant.FullName,
        BibNumber = reg.BibNumber,
        CategoryId = reg.CategoryID,
        CategoryName = reg.Category.CategoryName,
        FinishTime = result.FinishTime,
        OverallPosition = result.OverallPosition,
        CategoryPosition = result.CategoryPosition,
        Status = result.Status
    };

    public static ResultResponse From(Result result) => From(result, result.Registration);
}