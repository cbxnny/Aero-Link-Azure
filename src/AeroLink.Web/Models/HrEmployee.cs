namespace AeroLink.Web.Models;

/// <summary>Fields consumed from the supplied HR API's GET /api/employees/{id} response.</summary>
public class HrEmployee
{
    /// <summary>Numeric employee ID issued by the HR system.</summary>
    public int EmployeeId { get; set; }
    /// <summary>Employee's first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Employee's last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Email matched against the employee's login input.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Authoritative HR job title used to select the application role.</summary>
    public string JobTitle { get; set; } = string.Empty;
    /// <summary>HR department displayed in the signed-in employee's claims.</summary>
    public string DepartmentName { get; set; } = string.Empty;
}
