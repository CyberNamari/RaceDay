using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "Password123";

    /// <summary>A fresh client with its own cookie jar, which means its own session.</summary>
    public static HttpClient NewClient(this CustomWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Registers a brand-new user with the given role and logs them in.</summary>
    public static async Task<(HttpClient Client, int UserId, string Email)> RegisterAndLoginAsync(
        this CustomWebApplicationFactory factory, string role)
    {
        var client = factory.NewClient();
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = Password,
            Role = role
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        return (client, body!.UserId, email);
    }

    public static async Task<int> CreateVenueAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/venues", new CreateVenueRequest
        {
            VenueName = "Test Park",
            Address = "1 Main Road",
            City = "Pretoria",
            Province = "Gauteng",
            Latitude = -25.74m,
            Longitude = 28.19m
        });
        response.EnsureSuccessStatusCode();
        var venue = await response.Content.ReadFromJsonAsync<VenueResponse>();
        return venue!.VenueId;
    }

    public static CreateEventRequest NewEventRequest(int venueId) => new()
    {
        EventName = "Test Fun Run",
        EventType = "Run",
        EventDate = new DateOnly(2027, 6, 13),
        StartTime = new TimeOnly(5, 30),
        Description = "Test event",
        VenueId = venueId
    };

    public static async Task<EventDetailResponse> CreateEventAsync(HttpClient organiser)
    {
        var venueId = await CreateVenueAsync(organiser);
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(venueId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(HttpClient organiser, int eventId, int? maxParticipants = null)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest
        {
            CategoryName = "10km",
            DistanceKm = 10,
            EntryFee = 150,
            MaxParticipants = maxParticipants
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<(int EventId, int CategoryId)> CreateEventWithCategoryAsync(HttpClient organiser, int? maxParticipants = null)
    {
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId, maxParticipants);
        return (ev.EventId, category.CategoryId);
    }

    public static async Task<RegistrationResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/registrations", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegistrationResponse>())!;
    }
}