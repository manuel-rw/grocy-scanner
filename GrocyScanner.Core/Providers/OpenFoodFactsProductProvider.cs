using OpenFoodFacts4Net.ApiClient;
using OpenFoodFacts4Net.Json.Data;
using Product = GrocyScanner.Core.Models.Product;

namespace GrocyScanner.Core.Providers;

public class OpenFoodFactsProductProvider : IProductProvider
{
    public async Task<Product?> GetProductByGtin(string gtin)
    {
        Client client = new();
        try
        {
            GetProductResponse? product = await client.GetProductAsync(gtin);

            if (product == null)
            {
                return null;
            }

            return new Product
            {
                Gtin = product.Code,
                Name = product.Product.ProductName,
                ImageUrl = product.Product.ImageUrl,
                Categories = product.Product.CategoriesTags
            };
        }
        catch (HttpRequestException httpRequestException)
        {
            return null;
        }
    }
}