using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Enums;
using Shared.Helpers;
using Shared.Models.BaseEntity;
using Shared.Models.Trips;
using Shared.Models.Trucks;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    AffectedFields = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    AdditionalInfo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    PhoneNo = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    LicenseNo = table.Column<string>(type: "text", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Files = table.Column<List<UploadResult>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<Address>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<Address>(type: "jsonb", nullable: true),
                    IsDepot = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceSiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNo = table.Column<string>(type: "text", nullable: true),
                    HashedPassword = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ManagedProducts = table.Column<List<Product>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trucks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TruckNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LicensePlate = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VIN = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EngineNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Product = table.Column<int>(type: "integer", nullable: true),
                    CalibrationType = table.Column<int>(type: "integer", nullable: false),
                    CalibrationParams = table.Column<List<CalibrationParam>>(type: "jsonb", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Files = table.Column<List<UploadResult>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trucks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trucks_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EstimatedArrivalTime = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checkpoints_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TripDestinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArrivedAtStation = table.Column<bool>(type: "boolean", nullable: false),
                    StationArrivalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDischarged = table.Column<bool>(type: "boolean", nullable: false),
                    DischargeDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DischargeLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DischargedQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    DischargedUnit = table.Column<int>(type: "integer", nullable: true),
                    HasShortage = table.Column<bool>(type: "boolean", nullable: true),
                    ShortageAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripDestinations_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TripOrigins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripOrigins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripOrigins_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlannedTasks = table.Column<List<string>>(type: "jsonb", nullable: false),
                    ActualTasks = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Remark = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TomorrowPlans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Recommendations = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReports_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IoTs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    TruckId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoTs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IoTs_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefuelInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TruckId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchId = table.Column<string>(type: "text", nullable: true),
                    StationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefuelInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefuelInfos_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefuelInfos_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    TruckId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LoadingDepotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivingDepotId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoadingInfo = table.Column<LoadingInfo>(type: "jsonb", nullable: false),
                    ArrivalInfo = table.Column<ArrivalInfo>(type: "jsonb", nullable: false),
                    Metrics = table.Column<List<Metrics>>(type: "jsonb", nullable: true),
                    CloseInfo = table.Column<CloseInfo>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Stations_LoadingDepotId",
                        column: x => x.LoadingDepotId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Stations_ReceivingDepotId",
                        column: x => x.ReceivingDepotId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Users_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Discharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoicedStationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSiding = table.Column<bool>(type: "boolean", nullable: false),
                    SidingHours = table.Column<double>(type: "double precision", nullable: false),
                    TruckArrival = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DischargeStartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QuantityDischarged = table.Column<decimal>(type: "numeric", nullable: false),
                    IsFinalDischarge = table.Column<bool>(type: "boolean", nullable: false),
                    Files = table.Column<List<UploadResult>>(type: "jsonb", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discharges_Stations_InvoicedStationId",
                        column: x => x.InvoicedStationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Discharges_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Discharges_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TruckId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Incidents_IncidentTypes_IncidentTypeId",
                        column: x => x.IncidentTypeId,
                        principalTable: "IncidentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Incidents_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Incidents_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_Users_TreatedById",
                        column: x => x.TreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Item = table.Column<int>(type: "integer", nullable: false),
                    TruckId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceSiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_MaintenanceSites_MaintenanceSiteId",
                        column: x => x.MaintenanceSiteId,
                        principalTable: "MaintenanceSites",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Users_AssignedStaffId",
                        column: x => x.AssignedStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceRequest_Users_TreatedById",
                        column: x => x.TreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TripCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedDistanceFromPreviousKm = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualDistanceFromPreviousKm = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedArrivalTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualArrivalTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletionTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripCheckpoints_Checkpoints_CheckpointId",
                        column: x => x.CheckpointId,
                        principalTable: "Checkpoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripCheckpoints_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentHistories_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentHistories_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequestHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequestHistory_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceRequestHistory_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_StationId",
                table: "Checkpoints",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_Employee_Date",
                table: "DailyReports",
                columns: new[] { "EmployeeId", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_ReportDate",
                table: "DailyReports",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_Discharges_InvoicedStationId",
                table: "Discharges",
                column: "InvoicedStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Discharges_StationId",
                table: "Discharges",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Discharges_TripId",
                table: "Discharges",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentHistories_ChangedById",
                table: "IncidentHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentHistories_IncidentId",
                table: "IncidentHistories",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ClosedById",
                table: "Incidents",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedById",
                table: "Incidents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_DriverId",
                table: "Incidents",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentTypeId",
                table: "Incidents",
                column: "IncidentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TreatedById",
                table: "Incidents",
                column: "TreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TripId",
                table: "Incidents",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TruckId",
                table: "Incidents",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_IoTs_TruckId",
                table: "IoTs",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_RefuelInfos_StationId",
                table: "RefuelInfos",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_RefuelInfos_TruckId",
                table: "RefuelInfos",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_AssignedStaffId",
                table: "ServiceRequest",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_ClosedById",
                table: "ServiceRequest",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_CreatedById",
                table: "ServiceRequest",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_DriverId",
                table: "ServiceRequest",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_MaintenanceSiteId",
                table: "ServiceRequest",
                column: "MaintenanceSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_TreatedById",
                table: "ServiceRequest",
                column: "TreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_TripId",
                table: "ServiceRequest",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_TruckId",
                table: "ServiceRequest",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestHistory_ChangedById",
                table: "ServiceRequestHistory",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestHistory_ServiceRequestId",
                table: "ServiceRequestHistory",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckpoints_CheckpointId",
                table: "TripCheckpoints",
                column: "CheckpointId");

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckpoints_TripId",
                table: "TripCheckpoints",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripDestinations_StationId",
                table: "TripDestinations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripOrigins_StationId",
                table: "TripOrigins",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ClosedById",
                table: "Trips",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CompletedById",
                table: "Trips",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CreatedById",
                table: "Trips",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DispatchId",
                table: "Trips",
                column: "DispatchId",
                unique: true,
                filter: "\"DispatchId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId",
                table: "Trips",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_LoadingDepotId",
                table: "Trips",
                column: "LoadingDepotId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ReceivingDepotId",
                table: "Trips",
                column: "ReceivingDepotId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TruckId",
                table: "Trips",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_DriverId",
                table: "Trucks",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DailyReports");

            migrationBuilder.DropTable(
                name: "Discharges");

            migrationBuilder.DropTable(
                name: "IncidentHistories");

            migrationBuilder.DropTable(
                name: "IoTs");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "RefuelInfos");

            migrationBuilder.DropTable(
                name: "ServiceRequestHistory");

            migrationBuilder.DropTable(
                name: "TripCheckpoints");

            migrationBuilder.DropTable(
                name: "TripDestinations");

            migrationBuilder.DropTable(
                name: "TripOrigins");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "ServiceRequest");

            migrationBuilder.DropTable(
                name: "Checkpoints");

            migrationBuilder.DropTable(
                name: "IncidentTypes");

            migrationBuilder.DropTable(
                name: "MaintenanceSites");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "Trucks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Drivers");
        }
    }
}
