using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.GrocyClient;

public interface IGrocyClient
{
    public Task<int?> GetProductIdByBarcode(string gtin);

    public Task<Product?> GetProductByBarcode(string gtin);

    public Task<bool> UpsertProduct(Product product, int amount, DateOnly? bestBefore, double? price);
}