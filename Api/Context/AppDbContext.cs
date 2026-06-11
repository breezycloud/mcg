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

        base.OnModelCreating(modelBuilder);
    }

}