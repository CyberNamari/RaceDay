RaceDay
A full-stack, containerised, cloud-aware, API-driven event management system built for the South African road running, walking, and cycling community. Event Organisers can create and manage events, categories, and participant results. Participants can browse upcoming events, enter races, track their personal performance history, and prepare for race day using live weather and route information.

This project is built progressively across three parts as part of an individual Portfolio of Evidence (PoE).

How it works
RaceDay follows a standard three-layer, API-driven architecture:

Client (web/mobile UI)
        │
        ▼
  REST API layer   ──►  see docs/API_Endpoint_Plan.md for every route
        │
        ▼
SQL Server database ──►  see docs/ERD.png / docs/raceday_schema.sql for the data model
The client never talks to the database directly — every action goes through the API, which enforces roles and ownership before touching the data. This keeps the system consistent regardless of what front-end eventually calls it (web, mobile, or a future integration).

Organiser flow
Register / log in (POST /api/auth/register, POST /api/auth/login) as an Organiser.
Create an event (POST /api/events) at a chosen venue — e.g. a marathon, cycle tour, or community walk.
Add categories to that event (POST /api/events/{eventId}/categories) — e.g. 10km, 21km, Half Marathon — each with its own distance, entry fee, and participant cap.
Track who's entered (GET /api/events/{eventId}/registrations) as Participants sign up.
Capture results after the race (POST /api/registrations/{registrationId}/results), recording finish time and finishing position for each participant.
Participant flow
Register / log in as a Participant.
Browse events (GET /api/events) without needing to log in first, then drill into one for details (GET /api/events/{eventId}).
Check the weather for the event's venue (GET /api/events/{eventId}/weather) when deciding what to pack or wear on race day.
Enter a category (POST /api/categories/{categoryId}/registrations) to get a bib number and confirm payment status.
View personal history (GET /api/registrations/me) — every event entered, and the result once it's captured — and check the category leaderboard (GET /api/categories/{categoryId}/results) to see how they placed.
How the pieces connect
An Event always belongs to one Organiser and is hosted at one Venue.
An Event can have many Categories (different race distances/types within the same event).
A Participant enters a Category through a Registration — this is what resolves the real-world "many participants, many categories" relationship into a proper table, and it's also where the bib number and payment status live.
Each Registration produces at most one Result, captured once the race is run.
This flow is exactly what the ERD and API plan in /docs describe — Part 2's implementation should follow it step for step, and any place it doesn't will be called out below.

Repository structure
RaceDay/
├── docs/
│   ├── ERD.png                  # Section A — Entity Relationship Diagram
│   ├── API_Endpoint_Plan.md     # Section B — API endpoint plan
│   └── raceday_schema.sql       # Section C — full database schema + seed data
├── src/                          # Part 2 application code (added later)
└── README.md
Database
The schema lives in docs/raceday_schema.sql and matches docs/ERD.png exactly. It targets Microsoft SQL Server (SSMS).

To set it up:

Open SQL Server Management Studio and connect to your instance.
Create an empty database, e.g. CREATE DATABASE RaceDayDB; then USE RaceDayDB;
Run docs/raceday_schema.sql. It creates all six tables (Users, Venues, Events, Categories, Registrations, Results) with full constraints, then seeds sample data: 2 Organisers, 2 Participants, 3 Events, categories per event, and sample registrations/results.
Entities
Entity	Purpose
Users	Both Organisers and Participants, distinguished by a Role column
Venues	Physical locations where events are held
Events	Created by an Organiser, hosted at a Venue
Categories	Race categories within an event (e.g. 10km, Half Marathon)
Registrations	A Participant's entry into a Category
Results	Race-day outcome for a single Registration
Design decisions
Users is a single shared table for both Organisers and Participants (via the Role column) rather than two separate tables, since both share the same authentication and profile fields. This keeps login/auth logic in one place.
Registrations ↔ Results is enforced as strict 1:1 via a UNIQUE constraint on Results.RegistrationID — each race entry produces at most one result.
A participant cannot register twice for the same category — enforced with a UNIQUE constraint on (ParticipantID, CategoryID) in Registrations.
Any place where the implemented code in Part 2 deviates from this schema or from the API plan in docs/API_Endpoint_Plan.md will be explained in this section as it happens.

API
See docs/API_Endpoint_Plan.md for the full endpoint plan (Section B), covering Authentication, User Profile, Events, Categories, Registrations, and Results, plus one additional endpoint for live weather data to support race-day preparation.

Status
 Section A — ERD
 Section B — API Endpoint Plan
 Section C — SQL Database Script
 Part 2 — Implementation in c#

