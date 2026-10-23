using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>Signs in permitted staff after Employee ID/email validation through the supplied HR API (T2).</summary>
public class LoginModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLog;

    /// <summary>Creates the login page with HR validation and audit services.</summary>
    public LoginModel(IAuthService authService, IAuditLogService auditLog)
    {
        _authService = authService;
        _auditLog = auditLog;
    }

    /// <summary>Numeric HR Employee ID entered on the form.</summary>
    [BindProperty, Required(ErrorMessage = "Employee ID is required.")]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>Email which must match the employee's HR record.</summary>
    [BindProperty, Required(ErrorMessage = "Employee email is required."), EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Whether the employee explicitly asked for a persistent login cookie.</summary>
    [BindProperty]
    public bool RememberMe { get; set; }

    /// <summary>Optional local page requested before sign-in.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Actionable error returned by the HR validation service.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Displays the form or current identity. Does not automatically redirect an
    /// already signed-in Handler back to a Supervisor-only page after access denial.
    /// </summary>
    public IActionResult OnGet() => Page();

    /// <summary>Validates input and the HR record before recording sign-in and issuing a login cookie.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _authService.AuthenticateAsync(EmployeeId, Email);
        if (!result.Success || result.User is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Unable to validate these employee details.";
            return Page();
        }

        // Record the successful validation before issuing the sign-in cookie.
        await _auditLog.LogAsync("Login", result.User.FullName,
            $"{result.User.FullName} ({result.User.EmployeeId}) signed in via the HR API.", result.User.Role);
        await SignInUserAsync(result.User, RememberMe);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl);
        return RedirectToPage("/Index");
    }

    private async Task SignInUserAsync(AppUser user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.EmployeeId),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("EmployeeId", user.EmployeeId),
            new("Department", user.Department),
            new("RoleDisplayName", user.RoleDisplayName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), properties);
    }
}
