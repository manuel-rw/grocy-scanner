using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.Providers;

public class CoopProductProvider : IProductProvider
{
    public Task<Product?> GetProductByGtin(string gtin)
    {
        throw new NotImplementedException();
    }

    public string Name => "Coop";
    public string IconUri => "/coop-logo.png";

    public string Country => "Switzerland";
}