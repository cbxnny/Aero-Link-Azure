# AeroLink Ground Services - Baggage Handling Module

IAB251 Assessment 2 prototype. Working ASP.NET Core Razor implementation of the
Departure Baggage Loading Verification and Exception Management workflow, built
against the Module 5.2 user stories from Assignment 1.

## Sprint

- Sprint 1: 18 September 2026 to 25 October 2026
- Framework: ASP.NET Core Razor Pages
- Database: SQLite (see `src/AeroLink.Web/Data`)
- Auth: local demo accounts in `AuthService`; supplied HR API integration is a team task

## Structure

```
src/AeroLink.Web/      Razor Pages web app
  Pages/                One folder per functional area (Manifest, BagVerification,
                         Exceptions, SupervisorReview, LoadingClosure, Shared)
  Models/                Domain and view models
  Services/              Business logic / HR API client
  Data/                  DbContext, seed data, additive SQLite schema setup
  wwwroot/               Static assets

tests/AeroLink.Tests/    Unit tests (black-box, per assignment brief)
docs/                    Design carryover from Assignment 1, sprint notes
.github/workflows/       Build and test CI
```

## User stories in this module

| ID | Story | Owner |
| --- | --- | --- |
| I1 | View expected bags for a flight | Dinh (built) |
| I2 | Verify bag tag and confirm loading | Dinh (built) |
| I3 | Identify special baggage, require acknowledgement | Paul |
| I4 | Report missing/damaged baggage exception | Owen |
| I5 | Supervisor resolves exception / approves not-to-load | Owen |
| I6 | Supervisor closes loading | Paul |

Team-level stories (T1 to T4: main screen, login, logout, live status report)
are built collaboratively. This branch includes the home page, local demo
login/logout, and Paul's I3/I6 work. The I6 page requires sign-in and only
`BG_Supervisor` users may close loading. The supplied HR API integration and
the full T4 live report remain separate team work.

## Branching

See `docs/BRANCHING.md`. For Paul's feature branches, demo steps, and integration
review, see [I3/I6 handover](docs/I3-I6-HANDOVER.md).

## Getting started

```
cd src/AeroLink.Web
dotnet restore
dotnet run
```

The SQLite database is created and seeded automatically on first run from
`Data/Seed/baggage-manifest.json` (exported from the supplied
`IAB251_Synthetic_Baggage_Manifest.xlsx`). Startup adds the I6
`LoadingCompletions` table to existing databases without deleting their data.
Deleting `aerolink.db` resets local demo data; it is not required for this update.

Run tests from the repo root:

```
dotnet test
```

## What's implemented

- **I1** - `Pages/Manifest/Index.cshtml`: select a flight, view its manifest
  (tag, handling type, instructions, status), see loaded, approved-not-to-load,
  and outstanding counts.
- **I2** - `Pages/BagVerification/Verify.cshtml`: enter a bag tag, verify it
  against the selected flight's manifest, then confirm loading. Rejects
  invalid tags, tags from another flight, already-loaded bags, open exceptions,
  approved-not-to-load bags, and completed flights.
- **I3** - special bags display their category and supplied instructions.
  Loading requires an acknowledgement for that bag, checked by the server.
- **I4/I5** - report problems and record Supervisor decisions. These existing
  services now reject operational changes after I6 completes a flight.
- **I6** - `Pages/LoadingClosure/Index.cshtml`: reconcile the selected manifest,
  identify outstanding bags/open exceptions, and let a signed-in Supervisor
  close a ready flight. A final summary records outcomes, decisions, completion
  time and Supervisor identity. Completed flights remain available for review.
- Unit tests in `tests/AeroLink.Tests` cover verification, acknowledgement,
  exceptions, decisions, closure, authorization, and completed-flight protection.

## Team integration still to complete

- Replace local demo authentication with the supplied HR API integration and
  confirm access rules across the other operational pages.
- Complete the T4 live loading-status workflow. The I6 readiness page refreshes
  when requested and rechecks data on closure; it does not auto-refresh.
