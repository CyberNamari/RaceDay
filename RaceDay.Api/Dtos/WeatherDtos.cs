namespace RaceDay.Api.Dtos;

public class WeatherResponse
{
    /// <summary>Degrees Celsius.</summary>
    public decimal Temperature { get; set; }

    public string Condition { get; set; } = string.Empty;

    /// <summary>Kilometres per hour.</summary>
    public decimal WindSpeed { get; set; }

    /// <summary>The time the reading is for, in the venue's local time.</summary>
    public string ForecastTime { get; set; } = string.Empty;
}