using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Simulates the airline HR System API specified in the assessment brief.
/// Provides credential authentication and role verification for ground operations staff.
/// </summary>
public class AuthService : IAuthService
{
    private static readonly List<AppUser> SeedEmployees = new()
    {
        new AppUser
        {
            EmployeeId = "EMP-1001",
            Username = "supervisor",
            FullName = "Sarah Chen",
            Role = "BG_Supervisor",
            RoleDisplayName = "Baggage Supervisor",
            Department = "Ramp Operations Supervision",
            Password = "password123"
        },
        new AppUser
        {
            EmployeeId = "EMP-2001",
            Username = "handler",
            FullName = "David Miller",
            Role = "BG_Handler",
            RoleDisplayName = "Baggage Handler",
            Department = "Ramp Baggage Crew A",
            Password = "password123"
        },
        new AppUser
        {
            EmployeeId = "EMP-3001",
            Username = "lead",
            FullName = "Alex Taylor",
            Role = "BG_Supervisor",
            RoleDisplayName = "Ground Operations Lead",
            Department = "Ground Operations Command",
            Password = "password123"
        }
    };

    /// <inheritdoc />
    public Task<AuthResult> AuthenticateAsync(string usernameOrEmployeeId, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmployeeId))
        {
            return Task.FromResult(new AuthResult(false, "Employee ID or Username is required.", null));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(new AuthResult(false, "Password is required.", null));
        }

        var normalizedIdentifier = usernameOrEmployeeId.Trim();
        var user = SeedEmployees.FirstOrDefault(u =>
            string.Equals(u.Username, normalizedIdentifier, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.EmployeeId, normalizedIdentifier, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            return Task.FromResult(new AuthResult(false, "Invalid employee ID or username. Staff record not found in HR system.", null));
        }

        // Check password against HR record
        if (!string.Equals(user.Password, password, StringComparison.Ordinal))
        {
            return Task.FromResult(new AuthResult(false, "Incorrect password. Please verify your credentials.", null));
        }

        return Task.FromResult(new AuthResult(true, null, user));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AppUser>> GetDemoUsersAsync()
    {
        IReadOnlyList<AppUser> list = SeedEmployees.AsReadOnly();
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<AppUser?> FindUserAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return Task.FromResult<AppUser?>(null);
        }

        var normalized = identifier.Trim();
        var user = SeedEmployees.FirstOrDefault(u =>
            string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.EmployeeId, normalized, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }
}
