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
| I1 | View expected bags for a flight | TBC |
| I2 | Verify bag tag and confirm loading | TBC |
| I3 | Identify special baggage, require acknowledgement | TBC |
| I4 | Report missing/damaged baggage exception | TBC |
| I5 | Supervisor resolves exception / approves not-to-load | TBC |
| I6 | Supervisor closes loading | TBC |

Team-level stories (T1 to T4: main screen, login, logout, live status report)
are built collaboratively.

## Branching

See `docs/BRANCHING.md`.

## Getting started

Not yet scaffolded with actual project files. Next step: run
`dotnet new razor -o src/AeroLink.Web` and `dotnet new xunit -o tests/AeroLink.Tests`.
