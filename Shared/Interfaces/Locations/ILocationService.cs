using Shared.Helpers;

namespace Shared.Interfaces.Locations;

public interface ILocationService
{
    ValueTask<List<StateInfo>?> GetStatesWithLocalGovts(CancellationToken cancellationToken);

    ValueTask<List<string>?> States(CancellationToken cancellationToken);
    ValueTask<List<string>?> GetLocalGovtsByState(string stateName, CancellationToken cancellationToken);

}