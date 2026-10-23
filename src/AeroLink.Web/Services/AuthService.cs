using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Validates simulated login details against the supplied read-only HR REST API.</summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    /// <summary>Creates the service with the configured HR API HTTP client.</summary>
    public AuthService(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<AuthResult> AuthenticateAsync(string employeeId, string email)
    {
        var idText = (employeeId ?? string.Empty).Trim();
        var emailText = (email ?? string.Empty).Trim();
        if (!int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return new(false, "Enter a valid positive whole-number Employee ID.", null);

        if (!MailAddress.TryCreate(emailText, out var address) ||
            !string.Equals(address.Address, emailText, StringComparison.OrdinalIgnoreCase))
            return new(false, "Enter a valid employee email address.", null);

        try
        {
            using var response = await _httpClient.GetAsync($"api/employees/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(false, "Employee not found in the HR system.", null);
            if (!response.IsSuccessStatusCode)
                return new(false, "The HR system is unavailable. Please try again later.", null);

            var employee = await response.Content.ReadFromJsonAsync<HrEmployee>();
            if (employee is null || employee.EmployeeId != id ||
                string.IsNullOrWhiteSpace(employee.Email) || string.IsNullOrWhiteSpace(employee.JobTitle))
                return new(false, "The HR system returned an invalid employee record. Please contact your team.", null);

            if (!string.Equals(employee.Email.Trim(), emailText, StringComparison.OrdinalIgnoreCase))
                return new(false, "The email address does not match this Employee ID.", null);

            var role = employee.JobTitle.Trim() switch
            {
                var title when title.Equals("Baggage Handler", StringComparison.OrdinalIgnoreCase) => "BG_Handler",
                var title when title.Equals("BG_Supervisor", StringComparison.OrdinalIgnoreCase) => "BG_Supervisor",
                _ => null
            };
            if (role is null)
                return new(false, "Only Baggage Handlers and Baggage Supervisors can access this module.", null);

            var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
            return new(true, null, new AppUser
            {
                EmployeeId = id.ToString(CultureInfo.InvariantCulture),
                Email = employee.Email.Trim(),
                FullName = string.IsNullOrWhiteSpace(fullName) ? employee.Email.Trim() : fullName,
                Role = role,
                RoleDisplayName = role == "BG_Supervisor" ? "Baggage Supervisor" : "Baggage Handler",
                Department = employee.DepartmentName ?? string.Empty
            });
        }
        catch (HttpRequestException)
        {
            return new(false, "Cannot connect to the HR system. Check that the HR API is running and try again.", null);
        }
        catch (OperationCanceledException)
        {
            return new(false, "The HR system did not respond in time. Please try again.", null);
        }
        catch (JsonException)
        {
            return new(false, "The HR system returned an invalid employee record. Please contact your team.", null);
        }
        catch (NotSupportedException)
        {
            return new(false, "The HR system returned an unsupported response. Please contact your team.", null);
        }
    }
}
