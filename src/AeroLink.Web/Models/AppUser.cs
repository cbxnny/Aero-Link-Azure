namespace AeroLink.Web.Models;

/// <summary>
/// Represents an authenticated airline employee / ground operations staff member.
/// Corresponds to records retrieved from the HR System API.
/// </summary>
public class AppUser
{
    /// <summary>Numeric HR employee ID stored as text in the login claims (e.g., 100101).</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>Email verified against the supplied HR employee record.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Full display name of the staff member.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Security role code: "BG_Supervisor" (Supervisor) or "BG_Handler" (Baggage Handler).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Friendly title for display (e.g., "Baggage Supervisor").</summary>
    public string RoleDisplayName { get; set; } = string.Empty;

    /// <summary>Department or operations crew assignment.</summary>
    public string Department { get; set; } = string.Empty;

}
