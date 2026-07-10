using Microsoft.EntityFrameworkCore;
using Shared.Models.Drivers;
using Shared.Models.IoTs;
using Shared.Models.Logging;
using Shared.Models.Reports;
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
    public virtual DbSet<MotorMate> MotorMates { get; set; } = default!;
    public virtual DbSet<MotorMateHistory> MotorMateHistories { get; set; } = default!;
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
    public virtual DbSet<DailyReport> DailyReports { get; set; } = default!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email)
                  .IsUnique();

            // Self-referencing FK: who this user reports to. Restrict delete — an employee
            // record shouldn't vanish out from under their reports if the supervisor is
            // deleted; the supervisor must be reassigned/cleared first.
            entity.HasOne(x => x.Supervisor)
                  .WithMany()
                  .HasForeignKey(x => x.SupervisorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

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

            // Explicit plain FK index — without this, adding the filtered unique index below makes
            // EF Core treat it as the FK index's replacement and drop it. A partial index can't
            // serve ordinary "WHERE TruckId = x" lookups (used throughout Control Room/Dashboard/
            // Truck Report), so both must exist side by side. Two HasIndex() calls on the exact
            // same property are otherwise treated as the same index (identity is the property
            // list, not the chained .HasDatabaseName()) — passing the name directly here is what
            // makes EF Core keep them as genuinely separate indexes.
            entity.HasIndex(new[] { nameof(Trip.TruckId) }, "IX_Trips_TruckId");

            // A truck can only have one open trip (Active=0, Overdue=2, Dispatched=4) at a time.
            // Backs the app-level check in TripsController.ValidateNoOpenTripAsync as the final
            // safety net against two near-simultaneous dispatch requests both racing past it.
            entity.HasIndex(new[] { nameof(Trip.TruckId) }, "UX_Trips_TruckId_OpenStatus")
                  .IsUnique()
                  .HasFilter("\"Status\" IN (0, 2, 4)");
        });

        modelBuilder.Entity<DailyReport>(entity =>
        {
            entity.Property(x => x.ReportNo).HasMaxLength(20);

            // Store Department enum as integer (default EF behaviour — explicit for clarity)
            entity.Property(x => x.Department)
                  .HasConversion<int?>()
                  .IsRequired(false);

            // Composite UNIQUE index: one report per employee per date (enforced at DB level)
            // Also serves as the fast per-employee lookup index.
            entity.HasIndex(x => new { x.EmployeeId, x.ReportDate })
                  .IsUnique()
                  .HasDatabaseName("UX_DailyReports_Employee_Date");

            // Separate index for date-range report queries (Admin/Master viewing all)
            entity.HasIndex(x => x.ReportDate)
                  .HasDatabaseName("IX_DailyReports_ReportDate");

            entity.Property(x => x.WorkTasks).HasColumnType("jsonb");
            entity.Property(x => x.TomorrowTasks).HasColumnType("jsonb");

            entity.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedBy)
                  .WithMany()
                  .HasForeignKey(x => x.AssignedById)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);

            entity.HasOne(x => x.ReviewedBy)
                  .WithMany()
                  .HasForeignKey(x => x.ReviewedById)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasOne(x => x.CurrentMotorMate)
                  .WithMany(x => x.Drivers)
                  .HasForeignKey(x => x.CurrentMotorMateId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        modelBuilder.Entity<MotorMateHistory>(entity =>
        {
            entity.HasOne(x => x.Driver)
                  .WithMany()
                  .HasForeignKey(x => x.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PreviousMotorMate)
                  .WithMany()
                  .HasForeignKey(x => x.PreviousMotorMateId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);

            entity.HasOne(x => x.NewMotorMate)
                  .WithMany()
                  .HasForeignKey(x => x.NewMotorMateId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);

            entity.HasOne(x => x.ChangedBy)
                  .WithMany()
                  .HasForeignKey(x => x.ChangedById)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        base.OnModelCreating(modelBuilder);
    }

}