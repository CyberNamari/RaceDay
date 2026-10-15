using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EnrolmentTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndTheLinkIsRecorded()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (eventId, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, participantId, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);

        var response = await participant.PostAsync($"/api/categories/{categoryId}/registrations", null);
        var registration = await response.Content.ReadFromJsonAsync<RegistrationResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(participantId, registration!.ParticipantId);
        Assert.Equal(eventId, registration.EventId);
        Assert.Equal(categoryId, registration.CategoryId);
        Assert.Equal("1", registration.BibNumber);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        Assert.True(await db.Registrations.AnyAsync(r => r.ParticipantID == participantId && r.CategoryID == categoryId));
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        await TestHelpers.EnrolAsync(participant, categoryId);

        var second = await participant.PostAsync($"/api/categories/{categoryId}/registrations", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownCategory_Returns404()
    {
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);

        var response = await participant.PostAsync("/api/categories/999999/registrations", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser, maxParticipants: 1);
        var (first, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var (second, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        await TestHelpers.EnrolAsync(first, categoryId);

        var response = await second.PostAsync($"/api/categories/{categoryId}/registrations", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOwnEnrolments()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var registration = await TestHelpers.EnrolAsync(participant, categoryId);

        var mine = await participant.GetFromJsonAsync<List<RegistrationResponse>>("/api/registrations/me");

        var entry = Assert.Single(mine!);
        Assert.Equal(registration.RegistrationId, entry.RegistrationId);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (eventId, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, participantId, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        await TestHelpers.EnrolAsync(participant, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/registrations");
        var list = await response.Content.ReadFromJsonAsync<List<RegistrationResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(list!, r => r.ParticipantId == participantId);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEnrolments_Returns403()
    {
        var (owner, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (other, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (eventId, _) = await TestHelpers.CreateEventWithCategoryAsync(owner);

        var response = await other.GetAsync($"/api/events/{eventId}/registrations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEntry_ButNotSomeoneElses()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (owner, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var (other, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var registration = await TestHelpers.EnrolAsync(owner, categoryId);

        var byOther = await other.DeleteAsync($"/api/registrations/{registration.RegistrationId}");
        var byOwner = await owner.DeleteAsync($"/api/registrations/{registration.RegistrationId}");

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanSeeIt()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var registration = await TestHelpers.EnrolAsync(participant, categoryId);

        var captured = await organiser.PostAsJsonAsync($"/api/registrations/{registration.RegistrationId}/results", new ResultRequest
        {
            FinishTime = new TimeOnly(1, 45, 30),
            OverallPosition = 12,
            CategoryPosition = 3,
            Status = "Finished"
        });
        var mine = await participant.GetFromJsonAsync<List<RegistrationResponse>>("/api/registrations/me");

        Assert.Equal(HttpStatusCode.Created, captured.StatusCode);
        var entry = Assert.Single(mine!);
        Assert.NotNull(entry.Result);
        Assert.Equal(new TimeOnly(1, 45, 30), entry.Result!.FinishTime);
        Assert.Equal(3, entry.Result.CategoryPosition);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var registration = await TestHelpers.EnrolAsync(participant, categoryId);
        var body = new ResultRequest { FinishTime = new TimeOnly(0, 50, 0), OverallPosition = 1, CategoryPosition = 1, Status = "Finished" };
        await organiser.PostAsJsonAsync($"/api/registrations/{registration.RegistrationId}/results", body);

        var second = await organiser.PostAsJsonAsync($"/api/registrations/{registration.RegistrationId}/results", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var registration = await TestHelpers.EnrolAsync(participant, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/registrations/{registration.RegistrationId}/results",
            new ResultRequest { Status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureResult_Returns403()
    {
        var (owner, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (other, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Organiser);
        var (_, categoryId) = await TestHelpers.CreateEventWithCategoryAsync(owner);
        var (participant, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        var registration = await TestHelpers.EnrolAsync(participant, categoryId);

        var response = await other.PostAsJsonAsync($"/api/registrations/{registration.RegistrationId}/results",
            new ResultRequest { FinishTime = new TimeOnly(1, 0, 0), Status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}