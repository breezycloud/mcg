using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Api.Context;
using Shared.Dtos;
using Shared.Models.Drivers;
using Shared.Models.Trucks;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _context;

    public DriversController(AppDbContext context)
    {
        _context = context;
    }


    
    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Driver>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Driver> response = new();
        try
        {
            var query = _context.Drivers.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.FirstName!, pattern) || EF.Functions.ILike(x.LastName!, pattern)
                || EF.Functions.ILike(x.PhoneNo!, pattern));
            }

            response.Total = await query.CountAsync();


            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

            await foreach (var item in pagedQuery)
            {
                response.Data.Add(item);
            }


            return response;

            
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // GET: api/Drivers/validate?phone=&excludeId=
    [HttpGet("validate")]
    public async Task<ActionResult<PhoneValidationResult>> ValidatePhone(string phone, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberHelper.NormalizeForComparison(phone);
        if (normalized.Length == 0) return new PhoneValidationResult();

        var match = await _context.Drivers.AsNoTracking()
            .Where(x => x.Id != excludeId)
            .Where(x => x.PhoneNo != null && x.PhoneNo.EndsWith(normalized))
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new PhoneValidationResult { MatchedId = match };
    }

    // GET: api/Drivers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers()
    {
        return await _context.Drivers.AsNoTracking().Include(x => x.Trips).ToListAsync();
    }

    // GET: api/Drivers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Driver>> GetDriver(Guid id)
    {
        var driver = await _context.Drivers.Include(x => x.Trips).Include(x => x.CurrentMotorMate).FirstOrDefaultAsync(x => x.Id == id);

        if (driver == null)
        {
            return NotFound();
        }

        return driver;
    }

    // PUT: api/Drivers/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDriver(Guid id, Driver driver)
    {
        if (id != driver.Id)
        {
            return BadRequest();
        }

        var normalizedPhone = PhoneNumberHelper.NormalizeForComparison(driver.PhoneNo);
        var duplicate = await _context.Drivers.AsNoTracking()
            .Where(x => x.Id != id)
            .AnyAsync(x => x.PhoneNo != null && x.PhoneNo.EndsWith(normalizedPhone));
        if (duplicate)
        {
            return Conflict("Another driver is already registered with this phone number.");
        }

        if (driver.CurrentMotorMateId.HasValue)
        {
            var motorMateExists = await _context.MotorMates.AnyAsync(x => x.Id == driver.CurrentMotorMateId.Value);
            if (!motorMateExists)
            {
                return BadRequest("Selected motor mate was not found.");
            }
        }

        var previousMotorMateId = await _context.Drivers.AsNoTracking()
            .Where(x => x.Id == id).Select(x => (Guid?)x.CurrentMotorMateId).FirstOrDefaultAsync();

        _context.Entry(driver).State = EntityState.Modified;
        var currentUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        AddMotorMateHistoryIfChanged(id, previousMotorMateId, driver.CurrentMotorMateId, currentUserId, "Manual edit");

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DriverExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Last-resort race guard: two near-simultaneous edits both passed the phone-number
            // pre-check above before either committed — same fix pattern as TrucksController.
            _context.ChangeTracker.Clear();
            return Conflict("Another driver is already registered with this phone number.");
        }

        return NoContent();
    }

    // POST: api/Drivers
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Driver>> PostDriver(Driver driver)
    {
        var normalizedPhone = PhoneNumberHelper.NormalizeForComparison(driver.PhoneNo);
        var duplicate = await _context.Drivers.AsNoTracking()
            .AnyAsync(x => x.PhoneNo != null && x.PhoneNo.EndsWith(normalizedPhone));
        if (duplicate)
        {
            return Conflict("A driver is already registered with this phone number.");
        }

        if (driver.CurrentMotorMateId.HasValue)
        {
            var motorMateExists = await _context.MotorMates.AnyAsync(x => x.Id == driver.CurrentMotorMateId.Value);
            if (!motorMateExists)
            {
                return BadRequest("Selected motor mate was not found.");
            }
        }

        _context.Drivers.Add(driver);
        var currentUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var newUid) ? newUid : (Guid?)null;
        AddMotorMateHistoryIfChanged(driver.Id, previousMotorMateId: null, driver.CurrentMotorMateId, currentUserId, "Manual entry");

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Last-resort race guard: two near-simultaneous "Add Driver" submissions both passed
            // the phone-number pre-check above before either committed.
            _context.ChangeTracker.Clear();
            return Conflict("A driver is already registered with this phone number.");
        }

        return CreatedAtAction("GetDriver", new { id = driver.Id }, driver);
    }

    private void AddMotorMateHistoryIfChanged(Guid driverId, Guid? previousMotorMateId, Guid? newMotorMateId, Guid? changedById, string notes)
    {
        if (newMotorMateId == previousMotorMateId) return;

        _context.MotorMateHistories.Add(new MotorMateHistory
        {
            Id = Guid.NewGuid(),
            DriverId = driverId,
            PreviousMotorMateId = previousMotorMateId,
            NewMotorMateId = newMotorMateId,
            ChangedById = changedById,
            ChangedAt = DateTimeOffset.UtcNow,
            Notes = notes,
        });
    }
    
    // DELETE: api/Drivers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDriver(Guid id)
    {
        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null)
        {
            return NotFound();
        }

        _context.Drivers.Remove(driver);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool DriverExists(Guid id)
    {
        return _context.Drivers.Any(e => e.Id == id);
    }

    // POST: api/Drivers/import/preview — parses + resolves the CSV against
    // current DB state, but writes nothing. Same code path as commit, so the
    // review grid the user confirms matches exactly what commit will do.
    [Authorize(Roles = "Supervisor, Admin, Master, DriverSupervisor, Manager")]
    [HttpPost("import/preview")]
    public async Task<ActionResult<DriverImportPreviewResponse>> PreviewImport(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using var stream = file.OpenReadStream();
        var (rows, _, _, _, _, _) = await ProcessImportAsync(stream, commit: false, changedById: null, cancellationToken);

        return new DriverImportPreviewResponse { Rows = rows };
    }

    // POST: api/Drivers/import/commit
    [Authorize(Roles = "Supervisor, Admin, Master, DriverSupervisor, Manager")]
    [HttpPost("import/commit")]
    public async Task<ActionResult<DriverImportCommitResponse>> CommitImport(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var changedById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        using var stream = file.OpenReadStream();
        var (rows, created, updated, motorMatesCreated, motorMateHistoryEntries, trucksAssigned) =
            await ProcessImportAsync(stream, commit: true, changedById, cancellationToken);

        return new DriverImportCommitResponse
        {
            Rows = rows,
            CreatedCount = created,
            UpdatedCount = updated,
            SkippedCount = rows.Count(r => r.HasErrors),
            MotorMatesCreated = motorMatesCreated,
            MotorMateHistoryEntries = motorMateHistoryEntries,
            TrucksAssigned = trucksAssigned,
        };
    }

    // CSV columns (fixed position — header text is ignored, since "Phone Number"
    // appears twice: driver's and motor mate's):
    // 0 FirstName, 1 LastName, 2 PhoneNo, 3 LicenseNo, 4 ExpiryDate,
    // 5 MotorMateName, 6 MotorMatePhoneNo, 7 AssignedTruckPlate
    private async Task<(List<DriverImportRowDto> Rows, int Created, int Updated, int MotorMatesCreated, int MotorMateHistoryEntries, int TrucksAssigned)>
        ProcessImportAsync(Stream csvStream, bool commit, Guid? changedById, CancellationToken cancellationToken)
    {
        var rows = new List<DriverImportRowDto>();
        int created = 0, updated = 0, motorMatesCreated = 0, motorMateHistoryEntries = 0, trucksAssigned = 0;

        // Within-batch cache so two rows referencing the same brand-new motor
        // mate (e.g. one mate working with several drivers) resolve to a
        // single record instead of creating a duplicate per row.
        var motorMateCache = new Dictionary<string, MotorMate>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        int rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            var row = new DriverImportRowDto { RowNumber = rowNumber };

            string? Get(int index)
            {
                if (index >= csv.Parser!.Count) return null;
                var value = csv.GetField(index);
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            row.FirstName = ToTitleCase(Get(0));
            row.LastName = ToTitleCase(Get(1));
            row.PhoneNo = Get(2);
            row.LicenseNo = Get(3);
            row.ExpiryDateRaw = Get(4);
            row.MotorMateName = Get(5);
            row.MotorMatePhoneNo = Get(6);
            row.AssignedTruckPlate = Get(7);

            if (string.IsNullOrWhiteSpace(row.FirstName)) row.Errors.Add("First name is required.");
            if (string.IsNullOrWhiteSpace(row.LastName)) row.Errors.Add("Last name is required.");

            var normalizedPhone = PhoneNumberHelper.NormalizeForComparison(row.PhoneNo);
            if (normalizedPhone.Length != 10) row.Errors.Add("Phone number must be 10 or 11 digits.");

            if (string.IsNullOrWhiteSpace(row.LicenseNo)) row.Errors.Add("License number is required.");

            if (string.IsNullOrWhiteSpace(row.ExpiryDateRaw))
            {
                row.Errors.Add("Expiry date is required.");
            }
            else if (TryParseDate(row.ExpiryDateRaw, out var expiryDate))
            {
                row.ExpiryDate = expiryDate;
            }
            else
            {
                row.Errors.Add($"Could not parse expiry date '{row.ExpiryDateRaw}'.");
            }

            if (row.HasErrors)
            {
                rows.Add(row);
                continue;
            }

            var existingDriver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.PhoneNo != null && d.PhoneNo.EndsWith(normalizedPhone), cancellationToken);
            row.MatchedDriverId = existingDriver?.Id;

            // --- Resolve motor mate ---
            var mmPhoneNormalized = PhoneNumberHelper.NormalizeForComparison(row.MotorMatePhoneNo);
            bool motorMateNameGiven = !string.IsNullOrWhiteSpace(row.MotorMateName);
            bool motorMatePhoneGiven = mmPhoneNormalized.Length > 0;
            bool motorMateInfoComplete = motorMateNameGiven && motorMatePhoneGiven && mmPhoneNormalized.Length == 10;
            bool motorMateInfoBlank = !motorMateNameGiven && !motorMatePhoneGiven;

            MotorMate? motorMate = null;
            if (motorMateNameGiven && motorMatePhoneGiven && mmPhoneNormalized.Length != 10)
            {
                row.Warnings.Add("Motor mate phone number looks invalid — motor mate not linked.");
            }
            else if (motorMateInfoComplete)
            {
                if (!motorMateCache.TryGetValue(mmPhoneNormalized, out motorMate))
                {
                    motorMate = await _context.MotorMates
                        .FirstOrDefaultAsync(m => m.PhoneNo != null && m.PhoneNo.EndsWith(mmPhoneNormalized), cancellationToken);
                }

                if (motorMate is null)
                {
                    if (commit)
                    {
                        motorMate = new MotorMate
                        {
                            Id = Guid.NewGuid(),
                            Name = row.MotorMateName!,
                            PhoneNo = row.MotorMatePhoneNo,
                            CreatedAt = DateTimeOffset.UtcNow,
                        };
                        _context.MotorMates.Add(motorMate);
                        motorMateCache[mmPhoneNormalized] = motorMate;
                        motorMatesCreated++;
                    }
                    else
                    {
                        row.Warnings.Add("New motor mate will be created.");
                    }
                }
                else
                {
                    motorMateCache[mmPhoneNormalized] = motorMate;
                }
                row.MatchedMotorMateId = motorMate?.Id;
            }
            else if (!motorMateInfoBlank)
            {
                row.Warnings.Add("Motor mate name/phone incomplete — existing motor mate assignment left unchanged.");
            }

            // --- Resolve assigned truck ---
            Truck? truck = null;
            if (!string.IsNullOrWhiteSpace(row.AssignedTruckPlate))
            {
                var normalizedPlate = row.AssignedTruckPlate.Replace(" ", "");
                truck = await _context.Trucks
                    .FirstOrDefaultAsync(t => EF.Functions.ILike(t.LicensePlate.Replace(" ", ""), normalizedPlate), cancellationToken);

                if (truck is null)
                {
                    row.Warnings.Add($"Truck plate '{row.AssignedTruckPlate}' not found — driver left unassigned.");
                }
                else
                {
                    row.MatchedTruckId = truck.Id;
                    if (truck.DriverId.HasValue && truck.DriverId != existingDriver?.Id)
                    {
                        row.Warnings.Add($"Truck '{row.AssignedTruckPlate}' was assigned to another driver — reassigning.");
                    }
                }
            }

            rows.Add(row);

            if (!commit)
            {
                continue;
            }

            // --- Commit: create/update Driver ---
            Driver driver;
            Guid? previousMotorMateId;
            if (existingDriver is not null)
            {
                driver = existingDriver;
                previousMotorMateId = driver.CurrentMotorMateId;
                driver.FirstName = row.FirstName;
                driver.LastName = row.LastName;
                driver.PhoneNo = row.PhoneNo;
                driver.LicenseNo = row.LicenseNo;
                driver.ExpiryDate = row.ExpiryDate;
                driver.UpdatedAt = DateTimeOffset.UtcNow;
                updated++;
            }
            else
            {
                driver = new Driver
                {
                    Id = Guid.NewGuid(),
                    FirstName = row.FirstName,
                    LastName = row.LastName,
                    PhoneNo = row.PhoneNo,
                    LicenseNo = row.LicenseNo,
                    ExpiryDate = row.ExpiryDate,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                previousMotorMateId = null;
                _context.Drivers.Add(driver);
                created++;
            }

            Guid? newMotorMateId = motorMateInfoComplete ? motorMate?.Id
                : motorMateInfoBlank ? (Guid?)null
                : previousMotorMateId; // incomplete info this row -> leave as-is

            if (newMotorMateId != previousMotorMateId)
            {
                driver.CurrentMotorMateId = newMotorMateId;
                AddMotorMateHistoryIfChanged(driver.Id, previousMotorMateId, newMotorMateId, changedById, "Driver CSV import");
                motorMateHistoryEntries++;
            }

            if (!string.IsNullOrWhiteSpace(row.AssignedTruckPlate))
            {
                if (truck is not null)
                {
                    var otherTruck = await _context.Trucks
                        .FirstOrDefaultAsync(t => t.DriverId == driver.Id && t.Id != truck.Id, cancellationToken);
                    if (otherTruck is not null)
                    {
                        otherTruck.DriverId = null;
                        otherTruck.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                    truck.DriverId = driver.Id;
                    truck.UpdatedAt = DateTimeOffset.UtcNow;
                    trucksAssigned++;
                }
            }
            else if (existingDriver is not null)
            {
                var currentTruck = await _context.Trucks
                    .FirstOrDefaultAsync(t => t.DriverId == driver.Id, cancellationToken);
                if (currentTruck is not null)
                {
                    currentTruck.DriverId = null;
                    currentTruck.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        if (commit)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return (rows, created, updated, motorMatesCreated, motorMateHistoryEntries, trucksAssigned);
    }

    private static string? ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        string[] formats = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy", "dd-MM-yyyy", "yyyy/MM/dd"];
        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(raw, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
        }
        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
