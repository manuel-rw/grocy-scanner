using Microsoft.Extensions.Logging;
using OpenFoodFacts4Net.ApiClient;
using OpenFoodFacts4Net.Json.Data;
using Product = GrocyScanner.Core.Models.Product;

namespace GrocyScanner.Core.Providers;

public class OpenFoodFactsProductProvider : IProductProvider
{
    private readonly ILogger<OpenFoodFactsProductProvider> _logger;

    public OpenFoodFactsProductProvider(ILogger<OpenFoodFactsProductProvider> logger)
    {
        _logger = logger;
    }

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

            var returnproduct = new Product
            {
                Gtin = product.Code,
                Name = product.Product.ProductName,
                ImageUrl = product.Product.ImageUrl,
                Categories = product.Product.CategoriesTags
            };
            return returnproduct.Name == null ? null : returnproduct;
        }
        catch (HttpRequestException httpRequestException) when (httpRequestException.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Product not found in Open Food Facts: {Gtin}", gtin);
            return null;
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Unable to request {Gtin}", gtin);
            return null;
        }
    }

    public string Name => "Open Food Facts";
    public string IconUri => "/open-food-facts-logo.png";
    public string Country => "Global";
}