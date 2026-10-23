using AeroLink.Web.Data;
using AeroLink.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AeroLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AeroLinkDb")
        ?? "Data Source=aerolink.db"));

builder.Services.AddScoped<IManifestService, ManifestService>();
builder.Services.AddScoped<IBagVerificationService, BagVerificationService>();
builder.Services.AddScoped<IExceptionService, ExceptionService>();
builder.Services.AddScoped<ISupervisorReviewService, SupervisorReviewService>();
builder.Services.AddScoped<ILoadingClosureService, LoadingClosureService>();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    var baseUrl = builder.Configuration["HrApi:BaseUrl"] ?? "http://localhost:5000/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Require a new HR-validated sign-in instead of reusing an old demo-login cookie.
        options.Cookie.Name = "Elevair.HrSession";
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AeroLinkDbContext>();
    await DbSeeder.SeedAsync(db, app.Environment.ContentRootPath);
}

app.Run();

/// <summary>Exposes Program for WebApplicationFactory-based integration tests.</summary>
public partial class Program { }
