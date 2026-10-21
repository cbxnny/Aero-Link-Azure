using System.Security.Claims;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>
/// Handles employee sign-out, clearing the authentication session cookie (Story T3).
/// Logs the logout action to the audit trail before clearing the session.
/// </summary>
public class LogoutModel : PageModel
{
    private readonly IAuditLogService _auditLog;

    public LogoutModel(IAuditLogService auditLog)
    {
        _auditLog = auditLog;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LogLogoutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LogLogoutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }

    private async Task LogLogoutAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var name = User.Identity.Name ?? "Unknown";
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var empId = User.FindFirst("EmployeeId")?.Value ?? "";

            await _auditLog.LogAsync(
                "Logout",
                name,
                $"{name} ({empId}) signed out.",
                role);
        }
    }
}
