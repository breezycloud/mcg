using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Context;
using Api.Util;
using Shared.Enums;
using Shared.Models.Drivers;
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
        stopWatch = new();
        stopWatch.Start();
        if (db.Database.EnsureCreated())
        {
            AddUsers(db);
            await AddTrucks(db);
            await AddDrivers(db);
        }   
       
        await db.SaveChangesAsync();
        stopWatch.Stop();
        var ts = stopWatch.Elapsed;
        var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        Console.WriteLine($"Data Successfully Imported in {elapsedTime}");
        
    }

    private static (string firstName, string lastName) GetFirstLastName(string fullName)
    {

        // Split the name into parts
        string[] nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        string lastName = "";
        string firstName = "";

        if (nameParts.Length > 0)
        {
            // The first part is typically the last name
            firstName = nameParts[0];

            // The rest parts form the first name
            if (nameParts.Length > 1)
            {
                lastName = string.Join(" ", nameParts, 1, nameParts.Length - 1);
            }
        }
        return (firstName, lastName);
    }
    public record Drivers(string fullName, string phoneNo);

    private static async Task AddDrivers(AppDbContext db)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Api.Data.drivers.json");
            if (stream is null)
            {
                throw new FileNotFoundException();
            }

            Console.WriteLine("driver data found");

            stream.Position = 0;
            await foreach (var driver in JsonSerializer.DeserializeAsyncEnumerable<Drivers>(stream, options: null, cancellationToken: new CancellationTokenSource().Token))
            {
                if (driver != null)
                {
                    var fullName = GetFirstLastName(driver!.fullName);
                    Console.WriteLine("First Name: {0} Last Name: {1}", fullName.firstName, fullName.lastName);

                    await db.Drivers.AddAsync(new Driver
                    {
                        Id = Guid.NewGuid(),
                        FirstName = fullName.firstName,
                        LastName = fullName.lastName,
                        PhoneNo = driver.phoneNo,                        
                    });
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
            await foreach (var truck in JsonSerializer.DeserializeAsyncEnumerable<Truck>(stream, options: null, cancellationToken: new CancellationTokenSource().Token))
            {
                if (truck != null)
                {
                    Console.WriteLine("{0} {1} {2}", truck.TruckNo, truck.VIN, truck.EngineNo);
                    await db.Trucks.AddAsync(truck);
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
            }
        ]);

        db.SaveChanges();
    }
}