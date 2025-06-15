using System.Diagnostics;
using Api.Context;
using Api.Util;
using Shared.Enums;
using Shared.Models.Users;

namespace Api.Data;


public class SeedData
{
     private static Stopwatch? stopWatch;
    public static void EnsureSeeded(IServiceProvider services)
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
        }   
        stopWatch.Stop();
        var ts = stopWatch.Elapsed;
        var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        Console.WriteLine($"Data Successfully Imported in {elapsedTime}");
        
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