# RaceDay — API Endpoint Plan (Section B)

This plan covers Authentication, User Profile, Events, Categories, Event Enrolments (Registrations), and Results, as required. One additional endpoint (live weather) has been added to support the race-day preparation feature described in the project background.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Register a new user as either an Organiser or a Participant | Public | `{ fullName, email, password, role }` | `201 Created` — `{ userId, fullName, email, role }` |
| POST | `/api/auth/login` | Authenticate a user and issue a session/JWT token | Public | `{ email, password }` | `200 OK` — `{ token, userId, role }` |
| GET | `/api/users/me` | Retrieve the logged-in user's own profile | Organiser, Participant | — | `200 OK` — `{ userId, fullName, email, phoneNumber, role, createdAt }` |
| PUT | `/api/users/me` | Update the logged-in user's own profile details | Organiser, Participant | `{ fullName, phoneNumber, email }` | `200 OK` — updated user object |
| GET | `/api/events` | Browse/list all upcoming events (supports filters e.g. city, event type, date) | Public | — | `200 OK` — array of event summaries |
| GET | `/api/events/{eventId}` | Retrieve full details for a single event, including venue | Public | — | `200 OK` — `{ eventId, eventName, eventType, eventDate, startTime, description, status, venue }` |
| POST | `/api/events` | Create a new event | Organiser | `{ eventName, eventType, eventDate, startTime, description, venueId }` | `201 Created` — created event object |
| PUT | `/api/events/{eventId}` | Update an event's details (must be the owning Organiser) | Organiser | `{ eventName, eventType, eventDate, startTime, description, status }` | `200 OK` — updated event object |
| DELETE | `/api/events/{eventId}` | Cancel/delete an event (must be the owning Organiser) | Organiser | — | `204 No Content` |
| GET | `/api/events/{eventId}/categories` | List all race categories available for an event | Public | — | `200 OK` — array of category objects |
| POST | `/api/events/{eventId}/categories` | Add a new category (e.g. 10km, Half Marathon) to an event | Organiser | `{ categoryName, distanceKm, entryFee, maxParticipants }` | `201 Created` — created category object |
| PUT | `/api/categories/{categoryId}` | Update a category's details | Organiser | `{ categoryName, distanceKm, entryFee, maxParticipants }` | `200 OK` — updated category object |
| DELETE | `/api/categories/{categoryId}` | Remove a category from an event | Organiser | — | `204 No Content` |
| POST | `/api/categories/{categoryId}/registrations` | Enter (register) the logged-in Participant into a category | Participant | `{}` (participant taken from auth token) | `201 Created` — `{ registrationId, bibNumber, paymentStatus, registrationDate }` |
| GET | `/api/registrations/me` | Retrieve the logged-in Participant's full entry/performance history | Participant | — | `200 OK` — array of registration + result objects |
| GET | `/api/events/{eventId}/registrations` | List all participant registrations for an event (roster management) | Organiser | — | `200 OK` — array of registration objects |
| DELETE | `/api/registrations/{registrationId}` | Cancel the logged-in Participant's own registration | Participant | — | `204 No Content` |
| POST | `/api/registrations/{registrationId}/results` | Capture a finish result for a registration | Organiser | `{ finishTime, overallPosition, categoryPosition, status }` | `201 Created` — created result object |
| PUT | `/api/results/{resultId}` | Correct/update a previously captured result | Organiser | `{ finishTime, overallPosition, categoryPosition, status }` | `200 OK` — updated result object |
| GET | `/api/categories/{categoryId}/results` | Retrieve the results/leaderboard for a category | Public | — | `200 OK` — array of result objects, ordered by position |
| GET | `/api/results/{resultId}` | Retrieve a single result record | Public | — | `200 OK` — result object |
| GET | `/api/events/{eventId}/weather` | Fetch live weather for the event's venue, for race-day preparation | Organiser, Participant | — | `200 OK` — `{ temperature, condition, windSpeed, forecastTime }` |

## Notes
- **Role Required "Public"** means no authentication token is needed (supports browsing before signup).
- Ownership checks (e.g. an Organiser can only edit their own events/categories, a Participant can only cancel their own registration) are enforced in the service layer, not just by role.
- This plan will be revisited if Part 2 implementation reveals a gap — any deviation will be explained in the README as required.
