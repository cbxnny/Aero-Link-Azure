# Elevair - Baggage Handling Module

Elevair is the IAB251 Assessment 2 prototype for AeroLink Ground Services.
It is an ASP.NET Core Razor implementation of the
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
                         Exceptions, SupervisorReview, LoadingClosure, AuditLog, Shared)
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
are built collaboratively. The integrated application includes the home page,
local demo login/logout, loading-readiness polling, and an audit log.
Operational pages require sign-in; only `BG_Supervisor` users may decide
exceptions, close loading, or view the audit log. The public homepage still
needs its operational information hidden before sign-in, and the supplied
HR API integration remains outstanding.

## Branching

See `docs/BRANCHING.md`. For integrated baggage-handling rules, demo steps,
and validation evidence, see [I3/I6 integration guide](docs/I3-I6-HANDOVER.md).

## Getting started

Install the .NET 8 SDK, then run:

```sh
cd src/AeroLink.Web
dotnet restore
dotnet run
```

The SQLite database is created and seeded automatically on first run from
`Data/Seed/baggage-manifest.json` (exported from the supplied
`IAB251_Synthetic_Baggage_Manifest.xlsx`). Startup adds missing `Exceptions`,
`LoadingCompletions`, and `AuditLog` tables to existing databases without deleting
their data. Deleting `aerolink.db` resets local demo data and is not needed for
these updates.

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
  time and Supervisor identity. Completed flights remain available for review,
  printing, and CSV download.
- **Live readiness** - the loading-completion page polls current counts every
  five seconds and reloads when outstanding/open-exception counts change or
  loading completes. Closure always rechecks the current database state.
- **Audit log** - Supervisors can review operational events such as sign-in,
  sign-out, bag loading/rejection, exception decisions, and loading closure.
- Unit tests in `tests/AeroLink.Tests` cover verification, acknowledgement,
  exceptions, decisions, closure, authorization, and completed-flight protection.

The latest reviewed `main` revision, `02175ad`, built successfully and passed
**101 tests**. HTTP checks covered sign-in, role restrictions, baggage and
exception workflows, closure, CSV export, audit records, and report persistence
after restarting the application. This does not replace visual browser review.

## Team integration still to complete

- Replace local demo authentication with the supplied HR API integration and
  the required Employee ID/email validation.
- Hide flight details and operational totals on the signed-out homepage to
  satisfy T1. The main entry page and module navigation can remain public.
- Refresh live blocker details when the affected bags/exceptions change even
  if their counts stay the same. Until fixed, manually reload the readiness
  page to confirm those details. Server-side closure checks remain in place.
