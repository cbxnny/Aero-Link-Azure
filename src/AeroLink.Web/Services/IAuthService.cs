using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Result of an employee authentication attempt.
/// </summary>
public record AuthResult(bool Success, string? ErrorMessage, AppUser? User);

/// <summary>
/// Authentication service interface simulating the airline HR System API.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates employee credentials against the mock HR database.
    /// </summary>
    Task<AuthResult> AuthenticateAsync(string usernameOrEmployeeId, string password);

    /// <summary>
    /// Returns the list of pre-configured demo staff accounts for quick testing.
    /// </summary>
    Task<IReadOnlyList<AppUser>> GetDemoUsersAsync();

    /// <summary>
    /// Finds a user by their employee ID or username.
    /// </summary>
    Task<AppUser?> FindUserAsync(string identifier);
}
