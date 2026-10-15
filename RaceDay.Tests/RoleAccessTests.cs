using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Tests;

public class RoleAccessTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoleAccessTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static HttpRequestMessage BuildRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }
        return request;
    }

    // Not logged in: every protected endpoint must answer 401.
    [Theory]
    [InlineData("GET", "/api/users/me")]
    [InlineData("PUT", "/api/users/me")]
    [InlineData("POST", "/api/auth/logout")]
    [InlineData("POST", "/api/venues")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("POST", "/api/categories/1/registrations")]
    [InlineData("GET", "/api/registrations/me")]
    [InlineData("GET", "/api/events/1/registrations")]
    [InlineData("POST", "/api/registrations/1/results")]
    [InlineData("GET", "/api/events/1/weather")]
    public async Task ProtectedEndpoint_WhenNotLoggedIn_Returns401(string method, string url)
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Participant trying Organiser functionality: 403.
    [Theory]
    [InlineData("POST", "/api/venues")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("GET", "/api/events/1/registrations")]
    [InlineData("POST", "/api/registrations/1/results")]
    [InlineData("PUT", "/api/results/1")]
    public async Task OrganiserEndpoint_AsParticipant_Returns403(string method, string url)
    {
        var (participant, _, _) = await _factory.RegisterAndLoginAsync("Participant");

        var response = await participant.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Organiser trying Participant functionality: 403.
    [Theory]
    [InlineData("POST", "/api/categories/1/registrations")]
    [InlineData("GET", "/api/registrations/me")]
    [InlineData("DELETE", "/api/registrations/1")]
    public async Task ParticipantEndpoint_AsOrganiser_Returns403(string method, string url)
    {
        var (organiser, _, _) = await _factory.RegisterAndLoginAsync("Organiser");

        var response = await organiser.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Public endpoints must stay open to anonymous visitors.
    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/venues")]
    [InlineData("/api/events/1/categories")]
    [InlineData("/api/categories/1/results")]
    public async Task PublicEndpoint_WhenNotLoggedIn_IsNotBlocked(string url)
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.GetAsync(url);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}