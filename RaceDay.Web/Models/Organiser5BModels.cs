using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, StringLength(50), Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99), Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 99999.99), Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }
}

public class CategoriesPageViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class ResultInputModel
{
    public int EventId { get; set; }
    public int RegistrationId { get; set; }
    public int? ResultId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished";
}