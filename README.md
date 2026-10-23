# Elevair - Baggage Handling Module

Elevair is the IAB251 Assessment 2 prototype for AeroLink Ground Services.
It is an ASP.NET Core Razor implementation of the
Departure Baggage Loading Verification and Exception Management workflow, built
against the Module 5.2 user stories from Assignment 1.

## Sprint

- Sprint 1: 18 September 2026 to 25 October 2026
- Framework: ASP.NET Core Razor Pages
- Database: SQLite (see `src/AeroLink.Web/Data`)
- Auth: Employee ID/email validated through the supplied HR REST API (see `docs/HR-API-SETUP.md`)

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
are built collaboratively. The integrated application includes a public welcome
page, HR-backed login/logout, loading-readiness polling, and an audit log.
Flight details and operational totals are available only after sign-in.
Only `BG_Supervisor` users may decide exceptions, close loading, or view the
audit log. HR startup instructions and verified employee examples are in
[HR API setup](docs/HR-API-SETUP.md).

## Branching

See `docs/BRANCHING.md`. For integrated baggage-handling rules, demo steps,
and validation evidence, see [I3/I6 integration guide](docs/I3-I6-HANDOVER.md).

## Getting started

Install the .NET 8 SDK and start the supplied HR System separately before
logging in. Follow [HR API setup](docs/HR-API-SETUP.md) for the two-terminal
startup commands and the Employee ID/email examples. Then start Elevair:

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
  five seconds and reloads when blocker details change, even if their counts
  stay unchanged, or loading completes. Failed updates disable the closure
  button while polling retries. Closure always rechecks the current database state.
- **Audit log** - Supervisors can review operational events such as sign-in,
  sign-out, bag loading/rejection, exception decisions, and loading closure.
- Unit tests in `tests/AeroLink.Tests` cover verification, acknowledgement,
  exceptions, decisions, closure, authorization, and completed-flight protection.

The combined HR integration and `main` revision `167d44b` passed **129 tests**.
HTTP checks against the supplied HR API covered valid and failed sign-in, role
restrictions, baggage and exception workflows, closure, CSV export, audit
records, logout, API unavailability and report persistence after restarting
the application. This does not replace visual browser review.

## Remaining review and demonstration steps

- Review and merge this HR integration through its pull request.
- Run both applications on the team's recording machine and review the pages
  in a browser, including failed login, Handler/Supervisor access and I3/I6.
- Update Azure Boards and submission evidence, then rehearse the demonstration.
