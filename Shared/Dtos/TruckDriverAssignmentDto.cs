namespace Shared.Dtos;

public class TruckDriverAssignmentDto
{
    public Guid TruckId { get; set; }
    public Guid? DriverId { get; set; }
}