using System.Security.Claims;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>
/// Handles user login for AeroLink Ground Services, authenticating staff credentials
/// against the mock HR System API (Story T2).
/// </summary>
public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IReadOnlyList<AppUser> DemoAccounts { get; set; } = Array.Empty<AppUser>();

    public async Task<IActionResult> OnGetAsync()
    {
        DemoAccounts = await _authService.GetDemoUsersAsync();

        // If already logged in, redirect to return URL or home
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        DemoAccounts = await _authService.GetDemoUsersAsync();

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both employee username/ID and password.";
            return Page();
        }

        var result = await _authService.AuthenticateAsync(Username, Password);
        if (!result.Success || result.User is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Invalid login credentials.";
            return Page();
        }

        await SignInUserAsync(result.User, RememberMe);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Fast 1-click login for demonstration, evaluation, and assessment testing.
    /// </summary>
    public async Task<IActionResult> OnPostQuickLoginAsync(string employeeId)
    {
        DemoAccounts = await _authService.GetDemoUsersAsync();

        var user = await _authService.FindUserAsync(employeeId);
        if (user is null)
        {
            ErrorMessage = "Requested demo account could not be found.";
            return Page();
        }

        await SignInUserAsync(user, rememberMe: true);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    private async Task SignInUserAsync(AppUser user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.EmployeeId),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("EmployeeId", user.EmployeeId),
            new("Username", user.Username),
            new("Department", user.Department),
            new("RoleDisplayName", user.RoleDisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }
}
