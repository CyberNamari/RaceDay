using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>
/// Starts the real API in memory, but swaps SQL Server for an in-memory database
/// and the live weather service for a fake, so tests run anywhere (including CI).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var dbRegistrations = services
                .Where(d => d.ServiceType.FullName != null
                         && d.ServiceType.FullName.Contains("DbContextOptions")
                         && d.ServiceType.FullName.Contains(nameof(RaceDayDbContext)))
                .ToList();
            foreach (var registration in dbRegistrations)
            {
                services.Remove(registration);
            }

            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IWeatherService>();
            services.AddSingleton<IWeatherService, FakeWeatherService>();
        });
    }
}

public class FakeWeatherService : IWeatherService
{
    public Task<WeatherInfo?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default) =>
        Task.FromResult<WeatherInfo?>(new WeatherInfo(21.5m, "Clear sky", 12m, "2027-06-13T05:30"));
}