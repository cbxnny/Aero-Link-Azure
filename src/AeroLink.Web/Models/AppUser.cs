namespace AeroLink.Web.Models;

/// <summary>
/// Represents an authenticated airline employee / ground operations staff member.
/// Corresponds to records retrieved from the HR System API.
/// </summary>
public class AppUser
{
    /// <summary>Unique employee ID code (e.g., EMP-1001).</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>System username for login.</summary>
    public string Username { get; set; } = string.Empty;

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

    /// <summary>Plaintext password or hash used for the dummy HR authentication.</summary>
    public string Password { get; set; } = string.Empty;
}
