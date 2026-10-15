using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;

public class WeatherTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WeatherTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Weather_WhenLoggedIn_Returns200_WithForecast()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);

        var response = await participant.GetAsync($"/api/events/{created.EventId}/weather");
        var weather = await response.Content.ReadFromJsonAsync<WeatherResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(21.5m, weather!.Temperature);
        Assert.Equal("Clear sky", weather.Condition);
    }

    [Fact]
    public async Task Weather_ForUnknownEvent_Returns404()
    {
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);

        var response = await participant.GetAsync("/api/events/999999/weather");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}