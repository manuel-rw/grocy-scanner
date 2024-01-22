using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.GrocyClient;

public interface IGrocyQuantityUnit
{
    public Task<IReadOnlyList<GrocyQuantityUnit>> GetQuantityUnits();
}