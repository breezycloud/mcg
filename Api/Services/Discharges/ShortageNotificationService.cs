using Microsoft.EntityFrameworkCore;
using Api.Context;
using Api.Services.Messages;
using RazorLight;
using Shared.Enums;
using Shared.Extensions;
using Shared.Models.Trips;
using Shared.Models.MessageBroker;
using Shared.Helpers;

namespace Api.Services.Discharges;

// Backs the "Preview & Send to CCU" button on ViewTrip.razor's Shortage & CCU card. Nothing is
// sent automatically anymore — GetPreviewAsync renders exactly what Send would mail out (or
// reports what's still missing), and SendAsync only fires on the user's explicit click, re-
// checking everything itself rather than trusting whatever the preview call last saw.
public class ShortageNotificationService
{
    private readonly AppDbContext _context;
    private readonly EmailPublisherService? _mailPublisher;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly RazorLightEngine _razorEngine;
    private readonly ILogger<ShortageNotificationService> _logger;

    #if RELEASE
        public ShortageNotificationService(AppDbContext context, EmailPublisherService mailPublisher, IConfiguration configuration, IWebHostEnvironment env, RazorLightEngine razorEngine, ILogger<ShortageNotificationService> logger)
        {
            _context = context;
            _mailPublisher = mailPublisher;
            _configuration = configuration;
            _env = env;
            _razorEngine = razorEngine;
            _logger = logger;
        }
    #endif

    #if DEBUG
        public ShortageNotificationService(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env, RazorLightEngine razorEngine, ILogger<ShortageNotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _razorEngine = razorEngine;
            _logger = logger;
        }
    #endif

    // Everything both GetPreviewAsync and SendAsync need: whether the discharge even qualifies,
    // what's still missing if not, and the fully-built message if it is ready. Kept private so
    // the two public entry points can't drift out of sync on what "ready" means.
    private class Context
    {
        public bool AlreadySent;
        public DateTimeOffset? SentAt;
        public List<string> Missing = [];
        public EmailQueueMessage? Message;
    }

    private async Task<Context?> BuildContextAsync(Guid dischargeId)
    {
        var discharge = await _context.Discharges
            .Include(d => d.Station)
            .Include(d => d.Trip).ThenInclude(t => t!.Discharges)
            .Include(d => d.Trip).ThenInclude(t => t!.Truck)
            .Include(d => d.Trip).ThenInclude(t => t!.Driver)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == dischargeId);

        if (discharge == null || !discharge.IsFinalDischarge) return null;
        if (discharge.ShortageNotifiedAt != null)
        {
            return new Context { AlreadySent = true, SentAt = discharge.ShortageNotifiedAt };
        }

        var trip = discharge.Trip;
        if (trip == null) return null;

        var loaded = trip.LoadingInfo?.Quantity ?? 0;
        var totalDischarged = trip.Discharges.Sum(d => d.QuantityDischarged);
        var shortage = loaded - totalDischarged;
        if (shortage <= 0) return null;

        // LPG (measured by weight, not volume ullage) and CNG never have reliable enough
        // shortage figures to act on — both are permanently excluded from the CCU pipeline,
        // independent of NotificationSettings.ExcludeCngFromShortage (that toggle only controls
        // whether CNG counts toward shortage dashboards/reports/aggregates, not this pipeline).
        var product = trip.Truck?.Product;
        if (product == Product.LPG || (product?.IsCng() ?? false)) return null;

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
            return new Context { Missing = missing };
        }

        // The DB-backed Settings page (Administration > Settings) is the real source of this
        // address now; the old Email:NrlCcu config key only survives as a last-resort fallback
        // for environments where nobody has opened that page yet.
        var nrlCcuAddress = !string.IsNullOrWhiteSpace(settings?.NrlCcuEmail)
            ? settings.NrlCcuEmail
            : _configuration["Email:NrlCcu"];
        if (string.IsNullOrWhiteSpace(nrlCcuAddress))
        {
            return new Context { Missing = ["an NRL CCU recipient address (set one on the Settings page)"] };
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

        return new Context { Message = message };
    }

    // Renders exactly what Send would mail out, without sending anything — safe to call as
    // often as the user opens the preview.
    public async Task<ShortagePreviewDto> GetPreviewAsync(Guid dischargeId)
    {
        var context = await BuildContextAsync(dischargeId);
        if (context == null)
        {
            return new ShortagePreviewDto { Ready = false, MissingRequirements = ["this discharge isn't a final discharge with a shortage yet"] };
        }
        if (context.AlreadySent)
        {
            return new ShortagePreviewDto { Ready = false, AlreadySent = true, SentAt = context.SentAt };
        }
        if (context.Message == null)
        {
            return new ShortagePreviewDto { Ready = false, MissingRequirements = context.Missing };
        }

        var templatePath = $"{context.Message.Template}.cshtml";
        var html = await _razorEngine.CompileRenderAsync(templatePath, context.Message.TemplateModel);

        return new ShortagePreviewDto
        {
            Ready = true,
            Subject = context.Message.Subject,
            To = context.Message.To,
            Cc = context.Message.Cc,
            Html = html
        };
    }

    // The only place an email actually gets queued now — explicitly triggered by the user
    // clicking "Send" on the preview, never automatically. Re-runs the full check itself rather
    // than trusting that a prior preview call is still accurate.
    public async Task<(bool Success, string? Error)> SendAsync(Guid dischargeId)
    {
        if (_mailPublisher == null)
        {
            return (false, "Email sending isn't available in this environment.");
        }

        var context = await BuildContextAsync(dischargeId);
        if (context == null)
        {
            return (false, "This discharge isn't a final discharge with a shortage.");
        }
        if (context.AlreadySent)
        {
            return (false, "A CCU notification was already sent for this discharge.");
        }
        if (context.Message == null)
        {
            return (false, $"Still missing: {string.Join(", ", context.Missing)}.");
        }

        _mailPublisher.QueueEmailAsync(context.Message);

        var discharge = await _context.Discharges.FirstAsync(d => d.Id == dischargeId);
        discharge.ShortageNotifiedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("CCU shortage notification sent for discharge {DischargeId}", dischargeId);
        return (true, null);
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
