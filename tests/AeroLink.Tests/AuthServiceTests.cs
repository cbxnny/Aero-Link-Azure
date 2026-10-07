using AeroLink.Web.Services;
using Xunit;

namespace AeroLink.Tests;

public class AuthServiceTests
{
    private readonly AuthService _service = new();

    [Fact]
    public async Task Authenticate_WithValidSupervisorCredentials_Succeeds()
    {
        var result = await _service.AuthenticateAsync("supervisor", "password123");

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.User);
        Assert.Equal("EMP-1001", result.User.EmployeeId);
        Assert.Equal("BG_Supervisor", result.User.Role);
        Assert.Equal("Sarah Chen", result.User.FullName);
    }

    [Fact]
    public async Task Authenticate_WithValidHandlerCredentials_Succeeds()
    {
        var result = await _service.AuthenticateAsync("handler", "password123");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("EMP-2001", result.User.EmployeeId);
        Assert.Equal("BG_Handler", result.User.Role);
        Assert.Equal("David Miller", result.User.FullName);
    }

    [Fact]
    public async Task Authenticate_WithEmployeeIdCaseInsensitive_Succeeds()
    {
        var result = await _service.AuthenticateAsync("emp-1001", "password123");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("Sarah Chen", result.User.FullName);
    }

    [Fact]
    public async Task Authenticate_WithWrongPassword_Fails()
    {
        var result = await _service.AuthenticateAsync("supervisor", "wrongPassword");

        Assert.False(result.Success);
        Assert.Contains("Incorrect password", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task Authenticate_WithNonexistentUser_Fails()
    {
        var result = await _service.AuthenticateAsync("unknown_user", "password123");

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("   ", "password123")]
    [InlineData("supervisor", "")]
    [InlineData("supervisor", "   ")]
    public async Task Authenticate_WithEmptyInputs_Fails(string username, string password)
    {
        var result = await _service.AuthenticateAsync(username, password);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetDemoUsers_ReturnsPresetAccounts()
    {
        var users = await _service.GetDemoUsersAsync();

        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Role == "BG_Supervisor");
        Assert.Contains(users, u => u.Role == "BG_Handler");
    }

    [Fact]
    public async Task FindUserAsync_FindsByUsernameOrEmployeeId()
    {
        var userByUsername = await _service.FindUserAsync("handler");
        var userByEmpId = await _service.FindUserAsync("EMP-2001");

        Assert.NotNull(userByUsername);
        Assert.NotNull(userByEmpId);
        Assert.Equal(userByUsername.EmployeeId, userByEmpId.EmployeeId);
    }
}
