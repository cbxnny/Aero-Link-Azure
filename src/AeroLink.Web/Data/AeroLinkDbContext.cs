using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Data;

/// <summary>
/// EF Core database context for the baggage handling prototype. Holds the
/// flight, bag and exception tables used across the Module 5.2 user stories.
/// </summary>
public class AeroLinkDbContext : DbContext
{
    /// <summary>Creates the context with EF Core's dependency-injected options.</summary>
    public AeroLinkDbContext(DbContextOptions<AeroLinkDbContext> options)
        : base(options)
    {
    }

    /// <summary>Departure flights with baggage manifests.</summary>
    public DbSet<Flight> Flights => Set<Flight>();

    /// <summary>Expected bags across all flights.</summary>
    public DbSet<Bag> Bags => Set<Bag>();

    /// <summary>Reported problems against bags (I4/I5).</summary>
    public DbSet<BaggageException> Exceptions => Set<BaggageException>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flight>()
            .HasMany(f => f.Bags)
            .WithOne(b => b.Flight)
            .HasForeignKey(b => b.FlightId);

        modelBuilder.Entity<Bag>()
            .HasIndex(b => b.Tag);

        modelBuilder.Entity<BaggageException>()
            .HasKey(e => e.ExceptionId);
            
        modelBuilder.Entity<BaggageException>()
            .HasOne(e => e.Bag)
            .WithMany()
            .HasForeignKey(e => e.BagId);

        base.OnModelCreating(modelBuilder);
    }
}
