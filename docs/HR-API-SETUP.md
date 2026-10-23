# T2 — supplied HR REST API setup and demonstration

## What is integrated

The application validates Employee ID and email using the lecturer-supplied
HR System API. The HR application runs separately, with its own SQLite database.
The baggage application uses HTTP requests to read HR employee records; it does
not access or copy the HR database into its own database.

For each login, `AuthService` sends `GET /api/employees/{id}`, verifies the returned
employee ID and email, and permits only the following HR job titles:

| HR `jobTitle` | Application role |
| --- | --- |
| `Baggage Handler` | `BG_Handler` |
| `BG_Supervisor` | `BG_Supervisor` |

Other HR employees cannot sign in to this baggage module. The server creates the
existing login claims from the returned employee record. The Supervisor's real
employee ID and name are retained in I6 closure records, final summaries and
audit events. This remains the assignment's simulated authentication; the
supplied API has no password-validation endpoint.

## Prerequisites

- .NET 8 SDK on the machine running both applications.
- Extract the lecturer's `HRSystem-2.zip` into a separate folder.
- Locate the folder containing `HRSystem.csproj`, `Program.cs` and
  `API_SPECIFICATION.md`. Do not move the HR project into the baggage project.
- Use the application's HR-integration branch or a reviewed version containing it.

Some included HR documentation still refers to a quotation module and older
sample IDs. The account examples below were verified against the supplied
source, packaged database, and running API.

## Terminal 1 — start the HR system

Open a VS Code terminal in the extracted folder containing `HRSystem.csproj`:

```powershell
dotnet run --project .\HRSystem.csproj --no-launch-profile --urls http://localhost:5000
```

Keep that terminal running. Open these local addresses in your browser:

```text
http://localhost:5000/swagger
http://localhost:5000/api/employees/100101
```

The second address should return Sarah Mitchell's employee record as JSON.
The explicit HTTP command uses the API's supported local-development HTTP
listener. HTTPS can also be used with a normally trusted development certificate;
no TLS certificate checks are disabled by the integration.

## Terminal 2 — start the baggage application

Open a second terminal in the `Aero-Link-Azure` repository root:

```powershell
dotnet run --project .\src\AeroLink.Web
```

Open the address shown after `Now listening on:`. On Login, enter the HR Employee
ID and matching email. The old username/password form and quick-login buttons
are removed. Existing demo-login cookies are not reused: this version uses a
new `Elevair.HrSession` cookie, requiring a fresh HR-validated login.

| Employee | Employee ID | Email | Role |
| --- | --- | --- | --- |
| Sarah Mitchell | `100101` | `s.mitchell@company.com` | Baggage Handler |
| James Cooper | `200100` | `j.cooper@company.com` | Supervisor |
| Priya Sharma | `300100` | `p.sharma@company.com` | Baggage Handler |

These are teaching-system employee records, not passwords or production accounts.
The “Keep me signed in” box controls persistence and is unchecked by default.
Signing out removes the application's HR session cookie.

## Configure another HR address

`src/AeroLink.Web/appsettings.json` contains:

```json
"HrApi": {
  "BaseUrl": "http://localhost:5000/"
}
```

Use the API server's root address, without `/api`; the client adds
`api/employees/{id}`. To override it just for the application terminal:

```powershell
$env:HrApi__BaseUrl = "http://localhost:5000/"
dotnet run --project .\src\AeroLink.Web
```

If you select another port when starting HR, use that same port here.
`localhost` refers to the machine running the application. Running one system
on a teammate's computer requires its reachable address and appropriate local
network configuration. Running both locally is the simplest demonstration setup.

## Failed login behavior

- Empty/non-numeric/non-positive Employee IDs and malformed emails are rejected.
- Unknown HR IDs show an employee-not-found message.
- A mismatched email does not sign the employee in.
- A matching employee with another job title, such as Developer, is rejected.
- Connection errors, a five-second timeout, HTTP failures or malformed employee
  responses show a useful error and do not issue a login cookie.
- There is no fallback to hardcoded demo users and no quick-login bypass.
- Supervisor-only pages remain unavailable to Handlers. An access denial shows
  the login/current-identity page without redirecting endlessly back to the
  same forbidden page.

## Tests and recording evidence

Run the full suite from the repository root:

```powershell
dotnet test
```

For just the HR validation cases:

```powershell
dotnet test --filter FullyQualifiedName~AuthServiceTests
```

`AuthServiceTests` contains 31 cases using an in-process HTTP handler. They
exercise the actual client request format and valid/invalid responses without
depending on a running external service. The full suite passed **121 tests**
after this integration.

Two-application HTTP validation against the supplied HR service also confirmed:

1. Employee-by-ID requests appear in the HR server logs.
2. Valid Handler and Supervisor details produce the expected access roles.
3. Wrong email, unknown ID, an unrelated job title, and the legacy quick-login
   request cannot sign in.
4. I3 acknowledgement and I6 closure still work with HR-backed identities.
5. The final report and CSV identify James Cooper with Employee ID `200100`.
6. Audit records, read-only closure protections and saved summaries survive the
   tested application restart. A fresh login was made after restarting.
7. Logout clears the session; stopping HR causes a connection error on a new
   login instead of signing the user in.

For the video, show the real HR API running, one valid login, one failed login,
the Handler-to-Supervisor transition, and a protected Supervisor action. Use the
actual test result of the final submitted build, which can increase after later
team changes. Visual browser review is still needed on the team's recording setup.

## Review and merge

This is the T2 integration branch, separate from the Elevair branding review.
The shared login interface now takes Employee ID/email instead of username/password;
the authenticated identity no longer stores a password or demo username.
Review `AuthService`, `IAuthService`, `AppUser`, the login page, the HTTP client
registration/configuration, and the updated tests together.

No changes were made to the supplied HR source or to the I3/I6 loading rules.
Existing baggage databases and completed records do not need to be reset.
When reconciling documentation with the branding branch, retain this HR setup
guide and replace the earlier handover's demo-login instructions with these
current steps. The remaining T1 homepage-data and T4 same-count blocker-refresh
issues are separate from this integration.
