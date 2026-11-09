# RaceDay

RaceDay is a race-event platform. Organisers create events and categories, confirm enrolments and publish results.
Participants browse events, enrol, and track their race history.

| Project | Description |
|---|---|
| `RaceDay.Api` | ASP.NET Core Web API (Part 2) with SQL Server, cookie authentication and image storage |
| `RaceDay.Web` | ASP.NET Core MVC front end (Part 3) that consumes the API |
| `RaceDay.Tests` | Automated API tests |

## Architecture

The MVC app has no database access and no business logic. Every operation goes through the API using `HttpClient`.
The API's session cookie is kept server-side in the MVC session. Images are uploaded from MVC to the API,
which stores them. MVC never talks to storage directly.

## Image storage

When `AzureStorage:ConnectionString` is set, the API stores images in Azure Blob Storage (container `raceday-images`).
When it is not set, the API saves images to `RaceDay.Api/wwwroot/uploads`. No code change is needed to switch.

## Prerequisites

.NET 8 SDK, SQL Server / LocalDB, Docker Desktop (to run the container).

## Run the API

1. Set the SQL connection string in `RaceDay.Api/appsettings.json` and apply migrations: `dotnet ef database update`
2. Optional, for Azure: `dotnet user-secrets set "AzureStorage:ConnectionString" "<connection string>"`
3. `dotnet run --project RaceDay.Api --launch-profile https` (this project uses `https://localhost:7258`)

## Run the MVC app locally

1. Set `Api:BaseUrl` in `RaceDay.Web/appsettings.json` to the API URL (keep the trailing slash), for example `https://localhost:7258/`.
2. `dotnet run --project RaceDay.Web --launch-profile https`

## Run the MVC app in Docker

With the API running, build the image and start the container:

    docker build -t raceday-web ./RaceDay.Web
    docker run --rm -p 8081:8080 -e Api__BaseUrl=https://host.docker.internal:7258/ -e Api__TrustAnyCertificate=true raceday-web

Then open http://localhost:8081.
`Api__TrustAnyCertificate=true` accepts the API's local development certificate and is for local use only.

## Test accounts

| Role | Email | Password |
|---|---|---|
| Organiser | organiser@raceday.test | Password123 |
| Participant | participant@raceday.test | Password123 |

## CI

GitHub Actions builds the API and MVC projects, runs the tests and builds the Docker image on every push.

## Demo video

[Watch the demonstration on YouTube](PASTE-YOUR-UNLISTED-YOUTUBE-LINK-HERE)