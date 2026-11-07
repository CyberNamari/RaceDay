# RaceDay

![CI](https://github.com/CyberNamari/RaceDay/actions/workflows/ci.yml/badge.svg)

RaceDay is an API-driven event management system for South African road running, walking and cycling events. Event Organisers create and manage events, categories and results. Participants browse events, enter them by choosing a category, track their results and check race-day weather.

This is an individual Portfolio of Evidence built in three parts:

- **Part 1:** ERD, API endpoint plan and SQL script (`/docs`)
- **Part 2 (this branch, `Part-2-api`):** ASP.NET Core Web API with Entity Framework Core, role-based access, Swagger and unit tests
- **Part 3:** MVC front end (not started)

## Contents

1. [Tech stack](#tech-stack)
2. [Repository structure](#repository-structure)
3. [Setup and run instructions](#setup-and-run-instructions)
4. [The two roles](#the-two-roles)
5. [Authentication and session management](#authentication-and-session-management)
6. [API endpoints](#api-endpoints)
7. [Database](#database)
8. [Unit tests](#unit-tests)
9. [CI/CD](#cicd)
10. [Demo video](#demo-video)
11. [Differences from the Part 1 plan](#differences-from-the-part-1-plan)

## Tech stack

- ASP.NET Core Web API (.NET 8), controllers
- Entity Framework Core 8, Code-First, SQL Server
- Cookie-based session authentication, passwords hashed with ASP.NET Core `PasswordHasher` (PBKDF2 with salt)
- Swagger UI (Swashbuckle) with XML documentation comments
- xUnit and `WebApplicationFactory` for tests (in-memory database)
- GitHub Actions for CI
- Open-Meteo (free, no key) for the weather endpoint

## Repository structure

```
RaceDay/
├── .github/workflows/ci.yml     # CI: build + unit tests
├── docs/
│   ├── ERD.png                  # Part 1 ERD
│   ├── API_Endpoint_Plan.md     # Part 1 endpoint plan (the specification)
│   ├── raceday_schema.sql       # Part 1 SQL script
│   └── ci-passing.png           # Green build screenshot
├── RaceDay.Api/
│   ├── Controllers/             # Auth, Users, Venues, Events, Categories, Registrations, Results, Weather
│   ├── Data/                    # RaceDayDbContext (maps the ERD)
│   ├── Dtos/                    # Request/response models with validation
│   ├── Extensions/              # Helper to read the logged-in user's id
│   ├── Migrations/              # EF Core InitialCreate migration
│   ├── Models/                  # Entities
│   └── Services/                # Weather service
├── RaceDay.Tests/               # xUnit tests
└── RaceDay.sln
```

## Setup and run instructions

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Express is fine) and, optionally, SQL Server Management Studio
- Visual Studio 2022, or the .NET CLI

### 1. Clone and switch branch

```
git clone https://github.com/CyberNamari/RaceDay.git
cd RaceDay
git checkout Part-2-api
```

### 2. Set the connection string

Open `RaceDay.Api/appsettings.json` and change the server name to your own SQL Server instance (the name you use to connect in SSMS). Backslashes must be doubled in JSON:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS01;Database=RaceDayApiDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Create the database

The database schema is created from the EF Core migration.

**Visual Studio:** open **Tools > NuGet Package Manager > Package Manager Console**, set the default project to `RaceDay.Api`, and run:

```
Update-Database
```

**Command line:**

```
dotnet tool install --global dotnet-ef
dotnet ef database update --project RaceDay.Api
```

### 4. Run the API

**Visual Studio:** press **F5**.

**Command line:**

```
dotnet run --project RaceDay.Api
```

Open the **https** address printed in the console and add `/swagger` (for example `https://localhost:7xxx/swagger`). Every endpoint can be tested there with no other tooling.

### 5. Try it out

1. `POST /api/auth/register` to create an Organiser (`"role": "Organiser"`) and a Participant (`"role": "Participant"`).
2. `POST /api/auth/login` as the Organiser. Swagger keeps the session cookie automatically.
3. `POST /api/venues`, then `POST /api/events`, then `POST /api/events/{eventId}/categories`.
4. `POST /api/auth/logout`, log in as the Participant, then `POST /api/categories/{categoryId}/registrations`.

### 6. Run the tests

```
dotnet test
```

Or in Visual Studio: **Test > Test Explorer > Run All Tests**. No SQL Server is needed for the tests.

## The two roles

Users choose their role when they register. It is stored on the account and cannot be changed through the profile endpoint.

### Organiser

- Creates venues, events and categories (distance or age categories, for example 10km, 21km, Under 20, Senior)
- Updates or deletes **their own** events and categories only
- Views every enrolment for their own events
- Captures and corrects finish times and positions for their own events
- Can view events, categories, results, their own profile and event weather

### Participant

- Browses events and categories
- Enters an event by selecting a category (one entry per event); the entry records the participant, event and category and assigns a bib number
- Views and cancels their own entries
- Views their own results and the public category leaderboards
- Can view their own profile and event weather

Organiser functionality is not accessible to a Participant and vice versa. Wrong role gives **403 Forbidden**. No session gives **401 Unauthorized**. Ownership is also enforced, so an Organiser cannot edit another Organiser's event.

## Authentication and session management

- Registration validates the details and stores a **hashed** password. The plain password is never stored or returned.
- Login creates a session: an HttpOnly cookie (`RaceDay.Session`) carrying the user's id and role. It expires after 2 hours of inactivity.
- Non-public endpoints require that session (`[Authorize]`), and role-restricted endpoints use `[Authorize(Roles = "...")]`.
- The API answers 401 and 403 directly instead of redirecting to a login page.
- `POST /api/auth/logout` ends the session.

## API endpoints

The full plan, with request bodies and responses, is in [`docs/API_Endpoint_Plan.md`](docs/API_Endpoint_Plan.md). Every endpoint is also documented in Swagger.

| Area | Method and route | Access |
|---|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | Public |
| Auth | `POST /api/auth/logout` | Any logged-in user |
| Profile | `GET /api/users/me`, `PUT /api/users/me` | Any logged-in user |
| Venues | `GET /api/venues` | Public |
| Venues | `POST /api/venues` | Organiser |
| Events | `GET /api/events`, `GET /api/events/{eventId}` | Public |
| Events | `POST /api/events`, `PUT /api/events/{eventId}`, `DELETE /api/events/{eventId}` | Organiser (owner for PUT and DELETE) |
| Categories | `GET /api/events/{eventId}/categories` | Public |
| Categories | `POST /api/events/{eventId}/categories`, `PUT /api/categories/{categoryId}`, `DELETE /api/categories/{categoryId}` | Organiser (owner) |
| Enrolments | `POST /api/categories/{categoryId}/registrations`, `GET /api/registrations/me`, `DELETE /api/registrations/{registrationId}` | Participant |
| Enrolments | `GET /api/events/{eventId}/registrations` | Organiser (owner) |
| Results | `POST /api/registrations/{registrationId}/results`, `PUT /api/results/{resultId}` | Organiser (owner) |
| Results | `GET /api/results/{resultId}`, `GET /api/categories/{categoryId}/results` | Public |
| Weather | `GET /api/events/{eventId}/weather` | Any logged-in user |

## Database

The schema is built with EF Core Code-First and matches the Part 1 ERD (`docs/ERD.png`) and SQL script (`docs/raceday_schema.sql`): six tables (`Users`, `Venues`, `Events`, `Categories`, `Registrations`, `Results`) with the same keys, foreign keys, check constraint on `Role`, defaults and unique constraints (unique email, one entry per participant per category, one result per registration).

## Unit tests

The tests in `RaceDay.Tests` start the real API in memory, with an in-memory database and a fake weather service, and make real HTTP calls with real session cookies. They cover expected success and expected failure cases for:

- **Registration and login:** valid and invalid registration, duplicate email, wrong password, unknown email, logout, profile view and update
- **Event management:** Organisers creating, updating and deleting their own events and categories; other Organisers being refused; validation of event type and venue
- **Role-based access:** every protected endpoint returns 401 when logged out; Organiser endpoints return 403 for Participants; Participant endpoints return 403 for Organisers; public endpoints stay open
- **Event enrolments:** a Participant can enrol and the link is recorded; duplicate and full-category entries are refused; Organisers can view enrolments for their own events only; results are captured and visible to the Participant
- **Weather:** success and unknown event

## CI/CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push. It checks the Part 1 documents exist, validates the SQL script, then restores, builds and runs all unit tests.

![Green CI build](docs/ci-passing.png)

## Demo video

[Watch the demo on YouTube (unlisted)](PASTE-YOUR-YOUTUBE-LINK-HERE)

## Differences from the Part 1 plan

Where the implementation differs from `docs/API_Endpoint_Plan.md`, the plan has been updated to match, and the reasons are:

- **Session cookie instead of a token:** the brief asks for session management, so login sets a session cookie rather than returning a JWT. The Part 1 plan allowed either ("session/JWT token").
- **Added endpoints:** `GET /api/venues` and `POST /api/venues` (an event needs a venue and the plan had no way to create one) and `POST /api/auth/logout` (a session needs a way to end).
- **Extra error responses:** 401 (no session), 403 (wrong role or not the owner), and 409 where an action conflicts with existing data (duplicate email, duplicate entry, full category, deleting an event or category that already has entries).
- **`Categories.DistanceKM` is nullable:** the brief allows age categories such as "Under 20" and "Senior", which have no distance. `docs/raceday_schema.sql` was changed to match.
- **Event types are Run, Walk and Cycle:** as the brief specifies. The seed data in `docs/raceday_schema.sql` was changed to match.
- **Distance lives on categories:** an event can offer several distances (10km, 21km), so distance is stored per category and returned with each event.
- **`GET /api/events` lists all events:** with optional filters (`city`, `eventType`, `date`) rather than only upcoming events.
- **One entry per event:** a Participant can enter an event once, through one category.
- **Weather:** uses Open-Meteo and returns 503 if the provider is unreachable.