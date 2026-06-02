using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Context;
using Api.Controllers;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Trips;
using Shared.Enums;

class Program
{
    static async Task<int> Main()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("validation-test")
            .Options;

        using var context = new AppDbContext(options);

        var controller = new TripsController(context);

        var method = typeof(TripsController).GetMethod("ValidateLoadingInfo", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            Console.WriteLine("ValidateLoadingInfo method not found.");
            return 2;
        }

        // Test 1: Single mode without destination (should produce validation message)
        var tripSingleNoDest = new Shared.Models.Trips.Trip
        {
            Id = Guid.NewGuid(),
            TruckId = Guid.NewGuid(),
            Date = DateTimeOffset.UtcNow,
            LoadingInfo = new LoadingInfo
            {
                DestinationMode = DestinationMode.Single,
                Destination = null
            }
        };

        var res1 = method.Invoke(controller, new object[] { tripSingleNoDest }) as string;
        Console.WriteLine($"Single w/o destination => {(res1 == null ? "OK" : "Validation: " + res1)}");

        // Test 2: Multi mode without destination (should be OK)
        var tripMultiNoDest = new Shared.Models.Trips.Trip
        {
            Id = Guid.NewGuid(),
            TruckId = Guid.NewGuid(),
            Date = DateTimeOffset.UtcNow,
            LoadingInfo = new LoadingInfo
            {
                DestinationMode = DestinationMode.Multi,
                Destination = null
            }
        };

        var res2 = method.Invoke(controller, new object[] { tripMultiNoDest }) as string;
        Console.WriteLine($"Multi w/o destination => {(res2 == null ? "OK" : "Validation: " + res2)}");

        return 0;
    }
}
