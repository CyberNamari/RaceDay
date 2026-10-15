using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EventTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var (organiser, organiserId, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var venueId = await TestHelpers.CreateVenueAsync(organiser);

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest(venueId));
        var created = await response.Content.ReadFromJsonAsync<EventDetailResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Test Fun Run", created!.EventName);
        Assert.Equal("Run", created.EventType);
        Assert.Equal(organiserId, created.OrganiserId);
        Assert.Equal("Scheduled", created.Status);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var venueId = await TestHelpers.CreateVenueAsync(organiser);
        var request = TestHelpers.NewEventRequest(venueId);
        request.EventType = "Marathon";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownVenue_Returns400()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest(venueId: 999999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", new UpdateEventRequest
        {
            EventName = "Renamed Ride",
            EventType = "Cycle",
            EventDate = new DateOnly(2027, 7, 1),
            StartTime = new TimeOnly(6, 0),
            Description = "Updated",
            Status = "Scheduled"
        });
        var updated = await response.Content.ReadFromJsonAsync<EventDetailResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed Ride", updated!.EventName);
        Assert.Equal("Cycle", updated.EventType);
    }

    [Fact]
    public async Task OtherOrganiser_CannotUpdateEvent_Returns403()
    {
        var (owner, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (other, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{created.EventId}", new UpdateEventRequest
        {
            EventName = "Hijacked",
            EventType = "Run",
            EventDate = new DateOnly(2027, 7, 1),
            StartTime = new TimeOnly(6, 0),
            Status = "Scheduled"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ThatDoesNotExist_Returns404()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);

        var response = await organiser.PutAsJsonAsync("/api/events/999999", new UpdateEventRequest
        {
            EventName = "Ghost",
            EventType = "Run",
            EventDate = new DateOnly(2027, 7, 1),
            StartTime = new TimeOnly(6, 0),
            Status = "Scheduled"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204_AndEventIsGone()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        var afterwards = await organiser.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotDeleteEvent_Returns403()
    {
        var (owner, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (other, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ThatHasRegistrations_Returns409()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (eventId, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        await TestHelpers.EnrolAsync(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanListAndViewEvents_WithoutLoggingIn()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(organiser);
        var anonymous = _factory.NewClient();

        var list = await anonymous.GetAsync("/api/events");
        var events = await list.Content.ReadFromJsonAsync<List<EventSummaryResponse>>();
        var single = await anonymous.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(events!, e => e.EventId == created.EventId);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_Returns404()
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddCategories_AndAgeCategoryNeedsNoDistance()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(organiser);

        var distance = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);
        var age = await organiser.PostAsJsonAsync($"/api/events/{created.EventId}/categories",
            new CategoryRequest { CategoryName = "Under 20", EntryFee = 100 });
        var ageBody = await age.Content.ReadFromJsonAsync<CategoryResponse>();

        Assert.Equal(10, distance.DistanceKm);
        Assert.Equal(HttpStatusCode.Created, age.StatusCode);
        Assert.Null(ageBody!.DistanceKm);
    }

    [Fact]
    public async Task OtherOrganiser_CannotAddCategory_Returns403()
    {
        var (owner, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (other, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PostAsJsonAsync($"/api/events/{created.EventId}/categories",
            new CategoryRequest { CategoryName = "5km", DistanceKm = 5, EntryFee = 50 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanViewCategories()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (eventId, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var anonymous = _factory.NewClient();

        var response = await anonymous.GetAsync($"/api/events/{eventId}/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(categories!, c => c.CategoryId == categoryId);
    }
}