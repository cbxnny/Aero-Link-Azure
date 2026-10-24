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
| I1 | View expected bags for a flight | Dinh (Ben) |
| I2 | Verify bag tag and confirm loading | Dinh (Ben) |
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

## Branching and team contributions

`main` contains the integrated prototype. The team used individual work branches
as well as branches for specific stories, fixes and shared tasks. The table
records branch names and contribution evidence; it does not imply that every
commit on an individual branch has been merged.

| Team member | Individual stories | Recorded branches and contribution evidence |
| --- | --- | --- |
| Dinh (Ben) | I1: flight manifest; I2: bag verification and loading | `benTest`, `audit-fix`; manifest/verification and tests (`49dcc1b`), homepage/login (`5c3c3ef`), audit logging (`6cc9fcb`), live polling/CSV/closed-flight and audit fixes (`cb09e6b`). |
| Owen | I4: report baggage exceptions; I5: Supervisor decisions | `Owen's-Work-Branch`; exception reporting, Supervisor decisions and tests (`31a8f3d`). The branch also contains a separate SVG icon update (`5d41cd9`) that is not yet integrated into `main`. |
| Paul | I3: special-handling acknowledgement; I6: loading closure | Initial `paul-work` branch, followed by `feature/US2.5.1-special-baggage` and `feature/I6-close-loading`; shared-task branches `feature/T2-hr-api-login`, `fix/homepage-and-live-status` and `fix/elevair-branding`. |

T1–T4 and shared UI, authentication, reporting and integration improvements were
completed collaboratively. The individual story assignments above remain the
same; Git history records the specific commits and their authors.

The following review branches are now integrated into `main`:

| Pull request | Branch | Integrated work |
| --- | --- | --- |
| [#1](https://github.com/cbxnny/Aero-Link-Azure/pull/1) | `feature/US2.5.1-special-baggage` | I3 acknowledgement and exception/loading integration guards |
| [#2](https://github.com/cbxnny/Aero-Link-Azure/pull/2) | `feature/I6-close-loading` | I6 reconciliation, closure, saved summaries and completed-record protection |
| [#3](https://github.com/cbxnny/Aero-Link-Azure/pull/3) | `fix/audit-and-auth-gaps` | Audit/authentication fixes and closure reporting improvements |
| [#5](https://github.com/cbxnny/Aero-Link-Azure/pull/5) | `fix/homepage-and-live-status` | Signed-out homepage access and live blocker-detail refresh |
| [#6](https://github.com/cbxnny/Aero-Link-Azure/pull/6) | `feature/T2-hr-api-login` | Supplied HR REST API login and current setup documentation |
| [#7](https://github.com/cbxnny/Aero-Link-Azure/pull/7) | `fix/elevair-branding` | Elevair branding and project documentation |

For new work, branch from an up-to-date `main`, use a story/task branch, make
meaningful commits, and open a pull request for another member to review before
merging. Link the story, commits and PR in Azure Boards where practical. See
[branching conventions](docs/BRANCHING.md) for the workflow. For integrated
baggage-handling rules, demo steps and validation evidence, see the
[I3/I6 integration guide](docs/I3-I6-HANDOVER.md).

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
- **I4/I5** - report problems and record Supervisor decisions. Supervisors can
  resolve an exception or approve a bag not to load, recording their decision
  reason. Operational changes are rejected after I6 completes a flight.
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

The integrated application, including HR login and the homepage/live-status
fixes, passed **129 tests**. These changes are on `main` following PR #6
(merge commit `8e2f367`).
HTTP checks against the supplied HR API covered valid and failed sign-in, role
restrictions, baggage and exception workflows, closure, CSV export, audit
records, logout, API unavailability and report persistence after restarting
the application. This does not replace visual browser review.

## Remaining review and demonstration steps

- Review the pending SVG icon update on Owen's branch and integrate it through
  a pull request if the team chooses to include it.
- Run both applications on the team's recording machine and review the pages
  in a browser, including failed login, Handler/Supervisor access and all I1–I6
  workflows.
- Update Azure Boards and submission evidence, then rehearse the demonstration.
