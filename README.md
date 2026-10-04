# AeroLink Ground Services - Baggage Handling Module

IAB251 Assessment 2 prototype. Working ASP.NET Core Razor implementation of the
Departure Baggage Loading Verification and Exception Management workflow, built
against the Module 5.2 user stories from Assignment 1.

## Sprint

- Sprint 1: 18 September 2026 to 25 October 2026
- Framework: ASP.NET Core Razor Pages
- Database: SQLite (see `src/AeroLink.Web/Data`)
- Auth: dummy HR System API (see Assessment brief appendix)

## Structure

```
src/AeroLink.Web/      Razor Pages web app
  Pages/                One folder per functional area (Manifest, BagVerification,
                         SpecialHandling, Exceptions, SupervisorReview, Shared)
  Models/                Domain and view models
  Services/              Business logic / HR API client
  Data/                  DbContext, seed data, EF migrations
  wwwroot/               Static assets

tests/AeroLink.Tests/    Unit tests (black-box, per assignment brief)
docs/                    Design carryover from Assignment 1, sprint notes
.github/workflows/       CI placeholder
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
are built collaboratively. This build includes a minimal `Pages/Index.cshtml`
stub for T1 with no auth, so I1/I2 can be reached and demoed on their own.

## Branching

See `docs/BRANCHING.md`.

## Getting started

```
cd src/AeroLink.Web
dotnet restore
dotnet run
```

The SQLite database is created and seeded automatically on first run from
`Data/Seed/baggage-manifest.json` (exported from the supplied
`IAB251_Synthetic_Baggage_Manifest.xlsx`). Delete `aerolink.db` to reseed.

Run tests from the repo root:

```
dotnet test
```

## What's implemented

- **I1** - `Pages/Manifest/Index.cshtml`: select a flight, view its manifest
  (tag, handling type, status), see loaded vs outstanding counts.
- **I2** - `Pages/BagVerification/Verify.cshtml`: enter a bag tag, verify it
  against the selected flight's manifest, then confirm loading. Rejects
  invalid tags, tags from another flight, already-loaded bags, and routes
  special-handling bags to the I3 acknowledgement step (not yet built) rather
  than allowing them straight through.
- Unit tests (`tests/AeroLink.Tests/BagVerificationServiceTests.cs`) cover
  I2's verification rules using an in-memory SQLite database.

## Not yet implemented

- I3, I4, I5, I6 (teammates), and T2 to T4 (login, logout, live status
  report). The `NeedsHandlingAcknowledgement` outcome in
  `BagVerificationOutcome` is the integration point I3 will hook into.
