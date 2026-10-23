using System.Security.Claims;
using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroLink.Tests;

/// <summary>Regression tests for T1: public entry without operational data until sign-in.</summary>
public class HomepageAccessTests
{
    [Fact]
    public async Task SignedOutVisitor_DoesNotQueryOperationalDatabase()
    {
        // No provider is configured. Any accidental operational query would fail.
        using var db = new AeroLinkDbContext(new DbContextOptions<AeroLinkDbContext>());
        var page = CreatePage(db, new ClaimsPrincipal(new ClaimsIdentity()));

        await page.OnGetAsync();

        Assert.Empty(page.Flights);
        Assert.Equal(0, page.TotalFlights);
        Assert.Equal(0, page.TotalBags);
        Assert.Equal(0, page.OpenExceptions);
    }

    [Fact]
    public async Task UnauthenticatedRoleClaim_DoesNotExposeOperationalData()
    {
        using var db = new AeroLinkDbContext(new DbContextOptions<AeroLinkDbContext>());
        var page = CreatePage(db, new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "BG_Supervisor")
        })));

        await page.OnGetAsync();

        Assert.Empty(page.Flights);
        Assert.Equal(0, page.TotalBags);
    }

    [Theory]
    [InlineData("BG_Handler")]
    [InlineData("BG_Supervisor")]
    public async Task SignedInEmployee_CanViewOperationalTotalsAndFlights(string role)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new AeroLinkDbContext(new DbContextOptionsBuilder<AeroLinkDbContext>()
            .UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Flights.Add(new Flight { FlightId = 1, FlightNumber = "DEMO101" });
        db.Bags.AddRange(new Bag { FlightId = 1, Tag = "PENDING" },
            new Bag { FlightId = 1, Tag = "LOADED", Outcome = "Loaded" });
        db.SaveChanges();
        var page = CreatePage(db, new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role)
        }, "TestCookie")));

        await page.OnGetAsync();

        Assert.Equal("DEMO101", Assert.Single(page.Flights).FlightNumber);
        Assert.Equal(1, page.TotalFlights);
        Assert.Equal(2, page.TotalBags);
        Assert.Equal(1, page.LoadedBags);
        Assert.Equal(50, page.LoadPercentage);
    }

    private static AeroLink.Web.Pages.IndexModel CreatePage(AeroLinkDbContext db, ClaimsPrincipal user) =>
        new(db) { PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } } };
}
