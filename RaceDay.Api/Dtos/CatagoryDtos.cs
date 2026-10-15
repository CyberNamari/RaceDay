using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Optional. Leave out for age categories such as "Under 20" or "Senior".</summary>
    [Range(0.01, 999.99)]
    public decimal? DistanceKm { get; set; }

    [Range(0, 99999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }

    public static CategoryResponse From(Category c) => new()
    {
        CategoryId = c.CategoryID,
        EventId = c.EventID,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKM,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };
}