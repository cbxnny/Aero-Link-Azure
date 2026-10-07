using AeroLink.Web.Data;
using AeroLink.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AeroLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AeroLinkDb")
        ?? "Data Source=aerolink.db"));

builder.Services.AddScoped<IManifestService, ManifestService>();
builder.Services.AddScoped<IBagVerificationService, BagVerificationService>();
builder.Services.AddScoped<IExceptionService, ExceptionService>();
builder.Services.AddScoped<ISupervisorReviewService, SupervisorReviewService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
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
