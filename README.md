# Elevair - Baggage Handling Module

Elevair is the IAB251 Assessment 2 prototype for AeroLink Ground Services.
It is an ASP.NET Core Razor implementation of the
Departure Baggage Loading Verification and Exception Management workflow, built
against the Module 5.2 user stories from Assignment 1.

## Project status

The implemented T1–T4 and I1–I6 workflows are integrated into `main`, together
with Elevair branding, SVG icons, Supervisor audit navigation and CSV exports.
The final verification on **25 October 2026** checked application revision
`63ef735`: **131 tests passed, 0 failed, 0 skipped**. This README update changes
documentation only; it does not alter the recorded application workflows.

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
| Dinh (Ben) | I1: flight manifest; I2: bag verification and loading | `benTest`, `audit-fix`; manifest/verification and tests (`49dcc1b`), homepage/login (`5c3c3ef`), audit logging (`6cc9fcb`), live polling/CSV/closed-flight and audit fixes (`cb09e6b`), audit navigation/UI cleanup (`aeb2924`) and expanded CSV export (`63ef735`). |
| Owen | I4: report baggage exceptions; I5: Supervisor decisions | `Owen's-Work-Branch`; exception reporting, Supervisor decisions and tests (`31a8f3d`). SVG icons were initially developed in `5d41cd9`; the final icon changes are integrated into `main` as `67430b0` from `fix-icons`. |
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
| [#8](https://github.com/cbxnny/Aero-Link-Azure/pull/8) | `docs/team-branching-readme` | Team branch/contribution documentation |

Subsequent integrated commits include the final icons (`67430b0`), audit
navigation/UI cleanup (`aeb2924`) and expanded CSV export (`63ef735`).

For new work, branch from an up-to-date `main`, use a story/task branch, make
meaningful commits, and open a pull request for another member to review before
merging. Link the story, commits and PR in Azure Boards where practical. See
[branching conventions](docs/BRANCHING.md) for the workflow. For integrated
baggage-handling rules, demo steps and validation evidence, see the
[I3/I6 integration guide](docs/I3-I6-HANDOVER.md).

## Getting started

Install the **.NET 8 SDK** and extract the lecturer-supplied HR System into a
separate folder. **Starting Elevair does not start the HR API. Both applications
must run on the same computer for the default localhost configuration.**

**Terminal 1 — in the folder containing `HRSystem.csproj`:**

```powershell
dotnet run --project .\HRSystem.csproj --no-launch-profile --urls http://localhost:5000
```

Keep it running. Check `http://localhost:5000/api/employees/100101`; it should
return the teaching employee record. Swagger is at `http://localhost:5000/swagger`.

**Terminal 2 — in the Elevair repository root:**

```powershell
dotnet restore
dotnet run --project .\src\AeroLink.Web\AeroLink.Web.csproj
```

Open the URL printed after `Now listening on:`. Keep both terminals running.
`HRSystem.csproj` is the API; `AeroLink.Web.csproj` is the website. These are two
separate projects and are expected to have different filenames.

| Employee | Employee ID | Email | Access |
| --- | --- | --- | --- |
| Sarah Mitchell | `100101` | `s.mitchell@company.com` | Handler |
| James Cooper | `200100` | `j.cooper@company.com` | Supervisor |
| Priya Sharma | `300100` | `p.sharma@company.com` | Handler |

Use the numeric Employee ID and matching email, not the former demo usernames
or passwords. Only the supported HR baggage job titles may sign in. If an
employee is not found, check the records served by your local
`http://localhost:5000/api/employees` and that Elevair targets the same API.
Do not delete the HR database to troubleshoot a login error.

`HrApi:BaseUrl` defaults to `http://localhost:5000/` in
`src/AeroLink.Web/appsettings.json`. For other ports, environment overrides,
role mapping and failed-login behaviour, see [HR API setup](docs/HR-API-SETUP.md).

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
- **T1–T3** - the public entry page provides module information without exposing
  flight records or operational totals. Employee ID/email login uses the HR API;
  logout clears the session. Handler and Supervisor roles control access.
- **T4 / live readiness** - the loading-completion page polls current counts every
  five seconds and reloads when blocker details change, even if their counts
  stay unchanged, or loading completes. Failed updates disable the closure
  button while polling retries. Closure always rechecks the current database state.
- **CSV export** - export a selected unfinished flight's current records, a
  completed flight's saved final summary, or all flights' baggage records.
  Downloads are available to signed-in employees. Completed summaries retain
  their saved outcomes and Supervisor identity.
- **Audit log** - Supervisors have a navigation link and can filter operational
  events by flight, action and user, including sign-in,
  sign-out, bag loading/rejection, exception decisions, and loading closure.
- Unit tests in `tests/AeroLink.Tests` cover verification, acknowledgement,
  exceptions, decisions, closure, authorization, and completed-flight protection.

## Final verification

Application revision **`63ef735`** was checked on **25 October 2026**:

- `dotnet test`: **131 passed, 0 failed, 0 skipped**.
- Two running applications, using the supplied HR API: all 12 employee records
  checked; the three supported baggage accounts signed in with the expected roles.
- Wrong email, unknown ID, unrelated job title, invalid input and the removed
  quick-login route did not grant access. Logout, remember-me and API-outage
  handling were checked.
- Public homepage data protection, rendered branding/SVG markup, protected
  operational pages and Supervisor-only audit access/navigation were checked.
- Verification and special-handling tests passed. HTTP checks exercised
  acknowledgement, exception reporting and decisions, blocked closure, and a
  completed flight with nine loaded bags and one approved not to load.
- Readiness revisions changed when different exceptions replaced each other
  without changing totals. Status responses disable caching.
- Live-flight CSV, all-flight CSV (100 bags across 10 flight records), saved
  completion CSV and HR Supervisor identity were checked.
- Completed-record protections, audit events and persistence after restarting
  the application were checked using isolated temporary databases.

These checks found no blocking failure in the exercised workflows. They are
service tests and HTTP-level checks, not automated visual browser validation or
an assessment of the team's video, report or Azure Boards submission evidence.
No application code, seed data or shared database records were changed during
this final review.

## Demonstration and data notes

- Run both applications before demonstrating login. The HR API remains a
  simplified teaching service; Employee ID/email validation is simulated
  authentication rather than a production security system.
- The seeded data contains two `DEMO101` departures: flight **A / ID 101** on
  5 October and **J / ID 110** on 6 October, with different aircraft. Select by
  flight label/date as well as number. Bag verification uses the selected
  internal flight ID. The seed data was retained unchanged.
- Loading closure is final. Use the prepared local demonstration records;
  deleting a database is not necessary to update the application.
- The repository documents the implemented prototype and its validation.
  Video recordings, reports and Azure Boards evidence are submitted separately
  according to the assessment instructions.
