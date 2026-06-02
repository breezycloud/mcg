using Microsoft.EntityFrameworkCore;
using Shared.Models.Drivers;
using Shared.Models.IoTs;
using Shared.Models.Logging;
using Shared.Models.Shops;
using Shared.Models.Stations;
using Shared.Models.Trips;
using Shared.Models.Trucks;
using Shared.Models.Users;
using Shared.Models.Services;
using Shared.Models.Checkpoints;
using Shared.Models.RefuelInfos;
using Shared.Models.TripCheckpoints;
using Shared.Models.Incidents;

namespace Api.Context;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public virtual DbSet<User> Users { get; set; } = default!;
    public virtual DbSet<AuditLog> AuditLogs { get; set; } = default!;
    public virtual DbSet<Driver> Drivers { get; set; } = default!;
    public virtual DbSet<LogMessage> Logs { get; set; } = default!;
    public virtual DbSet<MaintenanceSite> MaintenanceSites { get; set; } = default!;
    public virtual DbSet<Station> Stations { get; set; } = default!;
    public virtual DbSet<Trip> Trips { get; set; } = default!;
    public virtual DbSet<Checkpoint> Checkpoints { get; set; } = default!;
    public virtual DbSet<TripCheckpoint> TripCheckpoints { get; set; } = default!;
    public virtual DbSet<Origin> TripOrigins { get; set; } = default!;
    public virtual DbSet<Destination> TripDestinations { get; set; } = default!;
    public virtual DbSet<Discharge> Discharges { get; set; } = default!;
    public virtual DbSet<Truck> Trucks { get; set; } = default!;
    public virtual DbSet<IoT> IoTs { get; set; } = default!;
    public virtual DbSet<RefuelInfo> RefuelInfos { get; set; } = default!;

    public DbSet<Shared.Models.Services.ServiceRequest> ServiceRequest { get; set; } = default!;
    public DbSet<Shared.Models.Services.ServiceRequestHistory> ServiceRequestHistory { get; set; } = default!;
    public DbSet<Incident> Incidents { get; set; } = default!;
    public DbSet<IncidentType> IncidentTypes { get; set; } = default!;
    public DbSet<IncidentHistory> IncidentHistories { get; set; } = default!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        // Enforce constraints on Trip.DispatchId to avoid duplicate dispatches at the database level.
        // Use a varchar(30) length and a unique index (filtered to non-null values) as a final safety net.
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.Property(t => t.DispatchId)
                  .HasMaxLength(30);

            entity.HasIndex(t => t.DispatchId)
                  .IsUnique()
                  .HasDatabaseName("IX_Trips_DispatchId")
                  .HasFilter("\"DispatchId\" IS NOT NULL");
        });

        base.OnModelCreating(modelBuilder);
    }

}