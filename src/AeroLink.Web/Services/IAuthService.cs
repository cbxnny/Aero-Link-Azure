using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Result of an employee authentication attempt.
/// </summary>
public record AuthResult(bool Success, string? ErrorMessage, AppUser? User);

/// <summary>
/// Simulated authentication using Employee ID, email and role from the supplied HR REST API.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Looks up an employee by ID, matches their email, and permits only the
    /// Baggage Handler or BG_Supervisor HR job title. Failed requests never sign in a user.
    /// </summary>
    /// <param name="employeeId">Positive numeric ID entered on the login form.</param>
    /// <param name="email">Employee email to compare with the HR record.</param>
    /// <returns>The permitted HR employee, or an actionable failure message.</returns>
    Task<AuthResult> AuthenticateAsync(string employeeId, string email);
}
