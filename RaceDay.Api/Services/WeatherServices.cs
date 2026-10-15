using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Services;

public record WeatherInfo(decimal Temperature, string Condition, decimal WindSpeed, string ForecastTime);

public interface IWeatherService
{
    /// <summary>Returns the current weather at a location, or null if the weather provider cannot be reached.</summary>
    Task<WeatherInfo?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}

public class WeatherService : IWeatherService
{
    private readonly HttpClient _http;

    public WeatherService(HttpClient http)
    {
        _http = http;
    }

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        // InvariantCulture matters: a South African regional setting would write 29,6 instead of 29.6.
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,wind_speed_10m,weather_code&timezone=auto";

        try
        {
            var data = await _http.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken);
            if (data?.Current is null)
            {
                return null;
            }

            return new WeatherInfo(
                data.Current.Temperature,
                Describe(data.Current.WeatherCode),
                data.Current.WindSpeed,
                data.Current.Time);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Describe(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown"
    };

    internal class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public OpenMeteoCurrent? Current { get; set; }
    }

    internal class OpenMeteoCurrent
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("temperature_2m")]
        public decimal Temperature { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public decimal WindSpeed { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}