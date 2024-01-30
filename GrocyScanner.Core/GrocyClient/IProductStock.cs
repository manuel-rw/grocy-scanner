using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.GrocyClient;

public interface IProductStock
{
    public Task AddProductToStockAsync(int productId, int amount, long locationId, DateOnly? bestBefore, double? price);

    public Task<GrocyErrorMessage?> ConsumeProductAsync(int productId, int amount, bool spoiled);
}