using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.Providers;

public interface IProductProvider
{
    public Task<Product?> GetProductByGtin(string gtin);
}