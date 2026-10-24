using System.Net;
using System.Net.Http.Json;
using System.Text;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Xunit;

namespace AeroLink.Tests;

/// <summary>Black-box T2 tests for the supplied HR REST contract and failure-safe login.</summary>
public class AuthServiceTests : IDisposable
{
    private readonly RecordingHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly IAuthService _service;

    public AuthServiceTests()
    {
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://hr.test/") };
        _service = new AuthService(_httpClient);
    }

    private static HrEmployee Record(int id = 100101, string title = "Baggage Handler", string email = "s.mitchell@company.com") =>
        new() { EmployeeId = id, FirstName = "Sarah", LastName = "Mitchell", Email = email,
            JobTitle = title, DepartmentName = "Operations" };

    private static HttpResponseMessage Response(HrEmployee employee) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(employee) };

    [Theory]
    [InlineData(100101, "s.mitchell@company.com", "Baggage Handler", "BG_Handler")]
    [InlineData(200100, "j.cooper@company.com", "BG_Supervisor", "BG_Supervisor")]
    [InlineData(300100, "p.sharma@company.com", "Baggage Handler", "BG_Handler")]
    public async Task MatchingHrEmployee_MapsRoleAndUsesReadOnlyLookup(int id, string email, string title, string role)
    {
        _handler.Respond = _ => Response(Record(id, title, email));
        var result = await _service.AuthenticateAsync(id.ToString(), email);
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.User);
        Assert.Equal(id.ToString(), result.User!.EmployeeId);
        Assert.Equal(email, result.User.Email);
        Assert.Equal(role, result.User.Role);
        Assert.Equal("Sarah Mitchell", result.User.FullName);
        Assert.Equal("Operations", result.User.Department);
        Assert.Equal(HttpMethod.Get, _handler.Method);
        Assert.Equal($"http://hr.test/api/employees/{id}", _handler.Uri!.ToString());
        Assert.Equal(1, _handler.Requests);
    }

    [Fact]
    public async Task InputWhitespaceAndEmailCase_AreNormalised()
    {
        var result = await _service.AuthenticateAsync(" 100101 ", " S.MITCHELL@COMPANY.COM ");
        Assert.True(result.Success);
        Assert.Equal("s.mitchell@company.com", result.User!.Email);
    }

    [Theory]
    [InlineData("", "s.mitchell@company.com")]
    [InlineData(" ", "s.mitchell@company.com")]
    [InlineData("EMP-1001", "s.mitchell@company.com")]
    [InlineData("0", "s.mitchell@company.com")]
    [InlineData("-1", "s.mitchell@company.com")]
    [InlineData("1.5", "s.mitchell@company.com")]
    [InlineData("2147483648", "s.mitchell@company.com")]
    [InlineData("100101", "")]
    [InlineData("100101", " ")]
    [InlineData("100101", "not-an-email")]
    [InlineData("100101", "Sarah <s.mitchell@company.com>")]
    public async Task InvalidInput_IsRejectedBeforeAnyHttpRequest(string id, string email)
    {
        var result = await _service.AuthenticateAsync(id, email);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.User);
        Assert.Equal(0, _handler.Requests);
    }

    [Fact]
    public async Task WrongEmail_DoesNotSignInExistingEmployee()
    {
        var result = await _service.AuthenticateAsync("100101", "someone.else@company.com");
        Assert.False(result.Success);
        Assert.Contains("does not match", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task MissingEmployee_ReturnsNotFoundMessage()
    {
        _handler.Respond = _ => new(HttpStatusCode.NotFound);
        var result = await _service.AuthenticateAsync("999999", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Theory]
    [InlineData("Developer")]
    [InlineData("HR Manager")]
    [InlineData("BG_Handler")]
    public async Task UnpermittedHrJobTitle_IsRejected(string title)
    {
        _handler.Respond = _ => Response(Record(title: title));
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Contains("Only Baggage Handlers", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task HrHttpFailure_DoesNotFallBackToDemoUsers(HttpStatusCode status)
    {
        _handler.Respond = _ => new(status);
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Contains("unavailable", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task ConnectionFailure_ReturnsActionableMessage()
    {
        _handler.Respond = _ => throw new HttpRequestException("Connection refused");
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Contains("HR API is running", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task Timeout_DoesNotGrantAccess()
    {
        _handler.Respond = _ => throw new TaskCanceledException("Timed out");
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Contains("did not respond in time", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task MalformedOrIncompletePayload_DoesNotGrantAccess(string body)
    {
        _handler.Respond = _ => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Contains("invalid employee record", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task ResponseForDifferentEmployee_IsRejected()
    {
        _handler.Respond = _ => Response(Record(id: 200100));
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Null(result.User);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingEmailOrJobTitle_DoesNotGrantAccess(bool omitEmail)
    {
        var record = Record();
        if (omitEmail) record.Email = "";
        else record.JobTitle = "";
        _handler.Respond = _ => Response(record);
        var result = await _service.AuthenticateAsync("100101", "s.mitchell@company.com");
        Assert.False(result.Success);
        Assert.Null(result.User);
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => Response(Record());
        public int Requests { get; private set; }
        public Uri? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Uri = request.RequestUri;
            Method = request.Method;
            return Task.FromResult(Respond(request));
        }
    }
}
