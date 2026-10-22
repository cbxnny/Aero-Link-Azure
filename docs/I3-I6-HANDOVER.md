# I3 and I6 integration and demonstration guide

## Integrated stories and traceability

**I3: special baggage handling acknowledgement** and **I6: Supervisor closes
loading** are integrated into `main`. The I3 integration fix connects I2 bag
verification with I4/I5 exception reports and Supervisor decisions.

- [Pull request #1](https://github.com/cbxnny/Aero-Link-Azure/pull/1): I3 and the
  exception-loading integration fix, from `feature/US2.5.1-special-baggage`.
- [Pull request #2](https://github.com/cbxnny/Aero-Link-Azure/pull/2): I6 closure
  and final summaries, from `feature/I6-close-loading`.
- Subsequent team changes add audit logging, operational-page access controls,
  live status polling, CSV export, and closed-flight banners.

Azure Boards story **61** tracks I3 with tasks **84, 85, and 86**. Story **64**
tracks I6 with tasks **93, 94, and 95**. Link the merged PRs and relevant commits
to those work items as contribution and acceptance evidence. For future work,
continue the feature-branch, teammate-review, and PR workflow in
[`BRANCHING.md`](BRANCHING.md).

## Why the shared services changed

Before the integration fix, I2 could mark a bag Loaded while I4 still had an
Open exception, or overwrite I5's ApprovedNotToLoad outcome. Verification and
confirmation now reject both cases. Resolving an exception leaves its bag
Pending and allows normal loading; special bags still require acknowledgement.

I6 marks a flight Completed only when its non-empty manifest is entirely Loaded
or ApprovedNotToLoad and there are no Open exceptions. Closing rechecks these
conditions on the server, even if the displayed readiness has become stale.
An empty manifest is treated as missing data and blocks closure.

The existing loading, reporting and Supervisor-decision services also check
Completed before writing. Their checks and writes use SQLite transactions,
as does closure, to protect the final record from competing operational writes.
This is needed by the assignment's requirement that completed records remain
available for review but cannot be changed.

## Shared service contracts

- **Bag verification:** use the verification outcomes and completed-flight rejection.
  Existing callers of `ConfirmLoadedAsync(flightId, tag)` still work for
  standard bags. The optional third argument carries I3 acknowledgement.
  Existing invalid-tag, wrong-flight and duplicate rules remain covered by tests.
- **Exception handling:** retain `Open`, `Resolved` and `ApprovedNotToLoad`, plus
  the completed-flight checks in both decision methods and exception reporting.
  The existing decision semantics and recorded reasons remain intact.
- **Authentication:** retain authenticated `NameIdentifier` and role `BG_Supervisor` claims
  when replacing demo login with the supplied HR API integration. I6 uses the
  server's authenticated principal, not a name or role submitted by a form.

The current login uses local demo accounts. Supplied HR API integration and
Employee ID/email validation remain a T2 team task. Manifest, verification,
reporting, and readiness pages require authentication; Supervisor review and
audit pages also require `BG_Supervisor`. The public homepage still exposes
operational data and needs the remaining T1 access change.

## Database compatibility

No database reset is needed. `DbSeeder` adds missing `LoadingCompletions` and
`AuditLog` tables following the repository's existing additive SQLite setup
pattern. Existing flights, bags and exceptions are preserved. The completion
table has one record per flight:
completion time, Supervisor employee ID/name and a JSON final-summary snapshot.
The snapshot and Completed status are saved atomically.

Older Completed records without a saved summary stay read-only and show that
no final summary is available; the app does not fabricate an earlier report.

## Run and demonstrate

From the repository root, with the .NET 8 SDK installed:

```powershell
dotnet test
dotnet run --project .\src\AeroLink.Web
```

Open the address printed by the running app. Use the existing login page's
demo accounts: a handler for normal baggage work, and a Supervisor for closure.
Use an unfinished flight; closing is final. Do not delete shared demo data just
to repeat the demonstration.

1. **I3:** open a manifest and inspect a special bag's category and instructions.
   Verify that bag. Confirm loaded is disabled until its instructions are
   acknowledged. Changing the tag or flight requires fresh verification.
2. **Blocked closure:** open Loading Completion and select a flight with pending
   bags. The page lists its outstanding bag tags and any open exceptions.
3. **Exceptions:** report a problem for one pending bag. Loading is rejected
   while that exception is open. Resolve it to load normally, or approve it
   not to load with a reason. Both outcomes can be demonstrated on different bags.
4. **Ready flight:** load the remaining bags, acknowledging each special bag.
   The readiness page shows expected, loaded, approved-not-to-load, outstanding,
   and open-exception counts. Counts are polled every five seconds. The view
   reloads when outstanding/open-exception counts change or loading completes.
   Manually refresh if exception details change without a count change; see
   the remaining live-update issue below.
5. **Role check:** a handler can review readiness but cannot close loading.
6. **Closure:** as Supervisor, close loading. The final summary shows the flight,
   bag outcomes, exception decisions/reasons, completion time and Supervisor.
   Print summary uses the browser's print dialog, including Save as PDF where supported.
   Download CSV exports the saved final summary and its exception decisions.
7. **Read-only review:** reopen the completed flight from its manifest or Loading
   Completion. Further loading, reports or decisions are rejected. The saved
   report remains available after restarting the app.
8. **Audit trail:** as Supervisor, open `/AuditLog` and review/filter the recorded
   sign-ins, bag outcomes, exception decisions, and loading-closure event.

## Validation evidence

Black-box service tests use isolated SQLite databases and verify persisted data:

- `SpecialHandlingServiceTests`: categories/instructions, required acknowledgement,
  duplicates, wrong flights, invalid tags, standard bags and per-bag acknowledgement.
- `LoadingExceptionGuardTests`: open/approved outcomes, resolved exceptions,
  stale verification and unrelated bags.
- `LoadingClosureServiceTests`: readiness blockers, empty/unknown flights,
  Supervisor identity, mixed/all-loaded/all-approved outcomes, duplicate closure,
  and repeatable non-destructive database upgrade.
- `CompletedLoadingProtectionTests`: loading, reporting and both decision paths
  after closure, including durable summaries read through a new database context.
- `AuditLogServiceTests` and `LoadingSummaryCsvTests`: audit records, filtering,
  and CSV formatting/export content.

The original I3/I6 feature validation passed **84 tests**. The latest reviewed
integrated `main`, commit `02175ad`, built and passed **101 tests**.
HTTP checks exercised anonymous/handler/Supervisor access, a complete
ten-bag flight with nine loaded and one approved not to load, rejected later
writes, CSV download, audit records, and retention of the final report after
restarting the application.
Automated visual browser testing was unavailable; review the pages in a browser
as part of the team demonstration.

## Remaining integration issues

- Connect the supplied HR API and validate Employee ID/email against it (T2).
- Hide operational flight details and totals from the public homepage until
  sign-in (T1).
- The readiness poll only detects blocker-table changes through counts. If one
  open exception is resolved and another is raised between polls, the counts
  can remain identical while the displayed bag details become stale. Manual
  refresh retrieves the current details; closure independently rechecks the
  database. Add a change marker or refreshed blocker data to complete this
  aspect of the T4 live report.

These documented gaps are separate from the completed I3/I6 rules and still
need implementation and validation. Azure Boards updates, peer review, and
the recorded presentation remain part of the team submission evidence.
