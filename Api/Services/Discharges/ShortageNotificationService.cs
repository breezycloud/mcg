using Microsoft.EntityFrameworkCore;
using Api.Context;
using Api.Services.Messages;
using Shared.Enums;
using Shared.Extensions;
using Shared.Models.Trips;
using Shared.Models.MessageBroker;
using Shared.Helpers;

namespace Api.Services.Discharges;

// Centralizes the "is this final discharge's shortage ready to notify NRL CCU about" check so it
// can be re-run from every place that can complete one of the four requirements: the discharge
// itself (waybill / IsFinalDischarge), the trip (loading + arrival ullage metrics), and the truck
// (calibration chart). Each of those controllers calls in here after its own save; nothing is
// sent until all four are present, and it's a no-op once ShortageNotifiedAt is already set.
public class ShortageNotificationService
{
    private readonly AppDbContext _context;
    private readonly EmailPublisherService? _mailPublisher;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ShortageNotificationService> _logger;

    #if RELEASE
        public ShortageNotificationService(AppDbContext context, EmailPublisherService mailPublisher, IConfiguration configuration, IWebHostEnvironment env, ILogger<ShortageNotificationService> logger)
        {
            _context = context;
            _mailPublisher = mailPublisher;
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }
    #endif

    #if DEBUG
        public ShortageNotificationService(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env, ILogger<ShortageNotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }
    #endif

    // Re-check every not-yet-notified final discharge on trips using this truck — called after a
    // truck save in case that's what just added the calibration chart.
    public async Task CheckAndNotifyForTruckAsync(Guid truckId)
    {
        var dischargeIds = await _context.Discharges
            .Where(d => d.IsFinalDischarge && d.ShortageNotifiedAt == null && d.Trip!.TruckId == truckId)
            .Select(d => d.Id)
            .ToListAsync();

        foreach (var dischargeId in dischargeIds)
        {
            await CheckAndNotifyAsync(dischargeId);
        }
    }

    // Re-check this trip's not-yet-notified final discharge, if it has one — called after a trip
    // save in case that's what just added the loading/arrival ullage readings.
    public async Task CheckAndNotifyForTripAsync(Guid tripId)
    {
        var dischargeId = await _context.Discharges
            .Where(d => d.TripId == tripId && d.IsFinalDischarge && d.ShortageNotifiedAt == null)
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        if (dischargeId != Guid.Empty)
        {
            await CheckAndNotifyAsync(dischargeId);
        }
    }

    // A discharge just closed out a trip's discharging (or one of its requirements just showed
    // up) — check whether the trip came up short and, if every requirement is now in place,
    // notify NRL CCU with the truck's calibration chart and this discharge's waybill attached.
    public async Task CheckAndNotifyAsync(Guid dischargeId)
    {
        if (_mailPublisher == null) return;

        var discharge = await _context.Discharges
            .Include(d => d.Station)
            .Include(d => d.Trip).ThenInclude(t => t!.Discharges)
            .Include(d => d.Trip).ThenInclude(t => t!.Truck)
            .Include(d => d.Trip).ThenInclude(t => t!.Driver)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == dischargeId);

        if (discharge == null || !discharge.IsFinalDischarge || discharge.ShortageNotifiedAt != null) return;

        var trip = discharge.Trip;
        if (trip == null) return;

        var loaded = trip.LoadingInfo?.Quantity ?? 0;
        var totalDischarged = trip.Discharges.Sum(d => d.QuantityDischarged);
        var shortage = loaded - totalDischarged;
        if (shortage <= 0) return;

        // LPG (measured by weight, not volume ullage) and CNG never have reliable enough
        // shortage figures to act on — both are permanently excluded from the CCU pipeline,
        // independent of NotificationSettings.ExcludeCngFromShortage (that toggle only controls
        // whether CNG counts toward shortage dashboards/reports/aggregates, not this pipeline).
        var product = trip.Truck?.Product;
        if (product == Product.LPG || (product?.IsCng() ?? false)) return;

        var settings = await _context.AppSettings.FirstOrDefaultAsync();

        var calibrationChart = trip.Truck?.Files.LastOrDefault(f => f.Category == FileCategories.CalibrationChart);
        var waybill = discharge.Files.LastOrDefault(f => f.Category == FileCategories.Waybill);
        var hasLoadingUllage = trip.LoadingInfo?.Metrics != null && trip.LoadingInfo.Metrics.Any();
        // Arrival readings are taken later and in a hurry — the ullage figure alone (without a
        // liquid-height reading too) is enough to satisfy this requirement.
        var hasArrivalUllage = trip.Metrics?.Any(m => m.Ullage?.Quantity != null) ?? false;

        var missing = new List<string>();
        if (calibrationChart?.ServerFileName == null) missing.Add("calibration chart");
        if (waybill?.ServerFileName == null) missing.Add("discharge waybill");
        if (!hasLoadingUllage) missing.Add("loading ullage readings");
        if (!hasArrivalUllage) missing.Add("arrival ullage readings");

        if (missing.Count > 0)
        {
            _logger.LogInformation("Shortage notification for discharge {DischargeId} withheld — missing: {Missing}", dischargeId, string.Join(", ", missing));
            return;
        }

        // The DB-backed Settings page (Administration > Settings) is the real source of this
        // address now; the old Email:NrlCcu config key only survives as a last-resort fallback
        // for environments where nobody has opened that page yet.
        var nrlCcuAddress = !string.IsNullOrWhiteSpace(settings?.NrlCcuEmail)
            ? settings.NrlCcuEmail
            : _configuration["Email:NrlCcu"];
        if (string.IsNullOrWhiteSpace(nrlCcuAddress))
        {
            _logger.LogWarning("Shortage detected on trip {TripId} but no NRL CCU email is configured (Settings page or Email:NrlCcu) — skipping notification.", trip.Id);
            return;
        }

        var message = new EmailQueueMessage
        {
            To = nrlCcuAddress,
            Cc = settings?.NrlCcuCcEmails,
            // DispatchId is an internal-only identifier — never surfaced to CCU, in the
            // subject or anywhere else in the email.
            Subject = $"Delivery Shortage — {trip.Truck?.LicensePlate} — Waybill {trip.LoadingInfo?.WaybillNo}",
            Template = "TripDischargeShortage",
            Attachments =
            [
                new EmailAttachmentRef { ServerFileName = calibrationChart!.ServerFileName!, DisplayFileName = calibrationChart.FileName },
                new EmailAttachmentRef { ServerFileName = waybill!.ServerFileName!, DisplayFileName = waybill.FileName },
            ],
            TemplateModel = new ShortageNotificationBody
            {
                TruckNo = trip.Truck?.TruckNo,
                TruckPlate = trip.Truck?.LicensePlate,
                WaybillNo = trip.LoadingInfo?.WaybillNo,
                DriverName = trip.Driver?.ToString(),
                StationName = string.IsNullOrWhiteSpace(discharge.Station?.Address?.State)
                    ? discharge.Station?.Name
                    : $"{discharge.Station.Name}, {discharge.Station.Address.State}",
                LoadingDate = trip.LoadingInfo?.LoadingDate?.ToString("dd/MM/yyyy"),
                LoadedQuantity = loaded,
                DischargedQuantity = totalDischarged,
                Compartments = BuildCompartmentRows(trip, loaded),
                ShortageAmount = shortage,
                Unit = trip.GetUnit(),
                IsTestEnvironment = !_env.IsProduction()
            }
        };
        _mailPublisher.QueueEmailAsync(message);

        discharge.ShortageNotifiedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    // Loading-side ullage/LH/overall live on Trip.LoadingInfo.Metrics; arrival-side readings
    // are entered later (on the Discharge tab, against the same Compartment enum) into
    // Trip.Metrics. Neither list stores a per-compartment loaded quantity, so it's the even
    // split of the trip's total loaded quantity across however many compartments were loaded —
    // matches how the calibration chart itself is filled in by hand.
    private static List<ShortageCompartmentRow> BuildCompartmentRows(Trip trip, decimal loaded)
    {
        var loadingMetrics = trip.LoadingInfo?.Metrics ?? [];
        var arrivalMetrics = trip.Metrics ?? [];
        var qtyPerCompartment = loadingMetrics.Count > 0 ? loaded / loadingMetrics.Count : 0;

        return loadingMetrics.Select(m => m.Compartment)
            .Concat(arrivalMetrics.Select(m => m.Compartment))
            .Distinct()
            .OrderBy(c => c)
            .Select(c =>
            {
                var loadingRow = loadingMetrics.FirstOrDefault(m => m.Compartment == c);
                var arrivalRow = arrivalMetrics.FirstOrDefault(m => m.Compartment == c);
                return new ShortageCompartmentRow
                {
                    Compartment = ((int)c + 1).ToString(),
                    QuantityLoaded = qtyPerCompartment,
                    LoadingUllage = loadingRow?.Ullage?.Quantity,
                    LoadingLiquidHeight = loadingRow?.LiquidHeight?.Quantity,
                    LoadingOverall = loadingRow?.Overall,
                    ArrivalUllage = arrivalRow?.Ullage?.Quantity,
                    ArrivalLiquidHeight = arrivalRow?.LiquidHeight?.Quantity,
                    ArrivalOverall = arrivalRow?.Overall
                };
            })
            .ToList();
    }
}
