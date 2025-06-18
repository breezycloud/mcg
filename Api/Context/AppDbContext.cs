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
    public virtual DbSet<Origin> TripOrigins { get; set; } = default!;
    public virtual DbSet<Destination> TripDestinations { get; set; } = default!;
    public virtual DbSet<Truck> Trucks { get; set; } = default!;
    public virtual DbSet<IoT> IoTs { get; set; } = default!;

public DbSet<Shared.Models.Services.ServiceRequest> ServiceRequest { get; set; } = default!;

}