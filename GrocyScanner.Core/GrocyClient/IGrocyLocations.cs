using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.GrocyClient;

public interface IGrocyLocations
{
    public Task<IReadOnlyList<GrocyLocation>> GetLocationsAsync();
}