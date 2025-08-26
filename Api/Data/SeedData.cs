using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Context;
using Api.Util;
using Shared.Enums;
using Shared.Models.BaseEntity;
using Shared.Models.Drivers;
using Shared.Models.Stations;
using Shared.Models.Trucks;
using Shared.Models.Users;

namespace Api.Data;


public class SeedData
{
    private static Stopwatch? stopWatch;
    public static async Task EnsureSeeded(IServiceProvider services)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
       
        if (db.Database.EnsureCreated())
        {
            stopWatch = new();
            stopWatch.Start();
            AddUsers(db);            
            await AddDrivers(db);
            await AddTrucks(db);
            await AddStations(db);
            await db.SaveChangesAsync();
            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine($"Data Successfully Imported in {elapsedTime}");
        }
    }

    public record Drivers(Guid Id, string FirstName, string LastName,string PhoneNo, string LicensePlate);
    public static List<Drivers> KDrivers = [];
    private static async Task AddDrivers(AppDbContext db)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Api.Data.drivers.json") ?? throw new FileNotFoundException();

            stream.Position = 0;
            await foreach (var driver in JsonSerializer.DeserializeAsyncEnumerable<Drivers>(stream, options: null, cancellationToken: new CancellationTokenSource().Token))
            {
                if (driver != null)
                {
                    //var fullName = GetFirstLastName(driver!.fullName);
                    // Console.WriteLine("First Name: {0} Last Name: {1}", driver.FirstName, driver.LastName);
                    // Console.WriteLine("Id: {0} First Name: {1} Last Name: {2}", newDriver.Id, newDriver.FirstName, newDriver.LastName);
                    // await Task.Delay(1000);
                    var newDriver = driver with { Id = Guid.NewGuid(), FirstName = driver.FirstName, LastName = driver.LastName, PhoneNo = driver.PhoneNo, LicensePlate = driver.LicensePlate };
                    var d = new Driver
                    {
                        Id = newDriver.Id,
                        FirstName = newDriver.FirstName,
                        LastName = newDriver.LastName,
                        PhoneNo = newDriver.PhoneNo,                                                
                    };
                    await db.Drivers.AddAsync(d);
                    KDrivers.Add(newDriver);
                }
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    private static async Task AddTrucks(AppDbContext db)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Api.Data.trucks.json");
            if (stream is null)
            {
                throw new FileNotFoundException();
            }

            stream.Position = 0;
            Drivers? lastDriver = null;
            await foreach (var truck in JsonSerializer.DeserializeAsyncEnumerable<Truck>(stream, options: null, cancellationToken: new CancellationTokenSource().Token))
            {
                if (truck != null)
                {
                    Console.WriteLine("{0} {1} {2}", truck.TruckNo, truck.VIN, truck.EngineNo);
                    if (!KDrivers.Any(x => x.LicensePlate == truck.LicensePlate))
                    {

                        truck.DriverId = null;
                    }
                    else
                    {
                        lastDriver = KDrivers.Where(x => x.LicensePlate == truck.LicensePlate).FirstOrDefault();
                        truck.DriverId = lastDriver?.Id;
                    }                    
                    await db.Trucks.AddAsync(truck);
                }
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    private static async Task AddStations(AppDbContext db)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Api.Data.stations.json") ?? throw new FileNotFoundException();

            stream.Position = 0;
            await foreach (var station in JsonSerializer.DeserializeAsyncEnumerable<Station>(stream, options: null, cancellationToken: new CancellationTokenSource().Token))
            {
                if (station != null)
                {
                    await db.Stations.AddAsync(station);
                }
            }
        }
        catch (System.Exception)
        {
            
            throw;
        }
    }

    private static void AddUsers(AppDbContext db)
    {
        db.Users.AddRange(
        [
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Aminu",
                LastName = "Aliyu",
                Email = "nerdyamin@gmail.com",
                HashedPassword = Security.Encrypt("jacubox123*"),
                Role = UserRole.Master
            },
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Mustapha",
                LastName = "Aliyu",
                Email = "mstphly@gmail.com",
                Role = UserRole.Master,
                HashedPassword = Security.Encrypt("12345678"),
            }
        ]);

        db.SaveChanges();
    }
}