using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static RegisterRequest NewRegisterRequest(string email, string role = RoleNames.Participant) => new()
    {
        FullName = "Test User",
        Email = email,
        Password = TestHelpers.Password,
        Role = role
    };

    [Fact]
    public async Task Register_WithValidDetails_Returns201_AndStoresHashedPassword()
    {
        var client = _factory.NewClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
    }

    [Fact]
    public async Task Register_AsOrganiser_StoresOrganiserRole()
    {
        var client = _factory.NewClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(UniqueEmail(), RoleNames.Organiser));
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(RoleNames.Organiser, body!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.NewClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email));

        var second = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = _factory.NewClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(UniqueEmail(), "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var client = _factory.NewClient();
        var request = NewRegisterRequest(UniqueEmail());
        request.Password = "short";

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200_WithRole()
    {
        var client = _factory.NewClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email, RoleNames.Organiser));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = TestHelpers.Password });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(RoleNames.Organiser, body!.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.NewClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.NewClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = UniqueEmail(), Password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.NewClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenLoggedIn_Returns200_AndCanBeUpdated()
    {
        var (client, userId, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);

        var view = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);

        var update = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FullName = "Updated Name",
            Email = $"updated-{Guid.NewGuid():N}@example.com",
            PhoneNumber = "0821234567"
        });
        var body = await update.Content.ReadFromJsonAsync<UserResponse>();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(userId, body!.UserId);
        Assert.Equal("Updated Name", body.FullName);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, _, _) = await _factory.RegisterAndLoginAsync(RoleNames.Participant);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }
}