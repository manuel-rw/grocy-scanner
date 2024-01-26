using System.Text.Json;
using GrocyScanner.Core.Extensions;
using GrocyScanner.Core.Models;
using GrocyScanner.Core.Models.Coop;
using Microsoft.Extensions.Logging;

namespace GrocyScanner.Core.Providers;

public class CoopProductProvider : IProductProvider
{
    private const string DesiredAnchor = "[data-product-container='searchresults']";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<CoopProductProvider> _logger;

    public CoopProductProvider(IHttpClientFactory httpClientFactory, ILogger<CoopProductProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Product?> GetProductByGtin(string gtin)
    {
        long timeInMilliseconds = DateTime.Now.GetUnixMilliseconds();
        using HttpClient httpClient = _httpClientFactory.CreateClient();

        // do not pass any headers. Coop's API is really picky
        HttpRequestMessage request = new(HttpMethod.Get,
            $"https://www.coop.ch/de/dynamic-pageload/searchresultJson?componentName=searchresultJson&url=%2Fde%2Fsearch%2F%3Ftext%3D{gtin}&displayUrl=%2Fde%2Fsearch%2F%3Ftext%3D{gtin}&compiledTemplates%5B%5D=productTile-new&compiledTemplates%5B%5D=sellingBanner&_={timeInMilliseconds}");

        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(request);
        string content = await httpResponseMessage.Content.ReadAsStringAsync();

        try
        {
            httpResponseMessage.EnsureSuccessStatusCode();
            CoopProduct coopProduct = JsonSerializer.Deserialize<CoopProduct>(content)!;

            if (!coopProduct.Success)
            {
                _logger.LogError("Coop indicated that result was not successful: {Json}", content);
                return null;
            }

            CoopProductAnhor? anchor = coopProduct.ContentJsons.Anchors.SingleOrDefault(anchor =>
                anchor.Anchor.Equals(DesiredAnchor, StringComparison.Ordinal));
            if (anchor == null)
            {
                _logger.LogError("Did not find the desired anchor for the product {Gtin}. Found {CountAnchor} anchors",
                    gtin, coopProduct.ContentJsons.Anchors.Count);
                return null;
            }

            if (anchor.Json.Elements.Count != 1)
            {
                _logger.LogError("Search returned more than one item for the barcode: {CountProducts}",
                    anchor.Json.Elements.Count);
                return null;
            }

            CoopProductElement element = anchor.Json.Elements.First();
            return new Product
            {
                Name = element.Title,
                Categories = element.UdoCat,
                Gtin = gtin,
                ImageUrl = GetBestQualityImage(element.Image)
            };
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Failed to fetch coop product: {Gtin} - {JsonResponse}", gtin,
                content);
            return null;
        }
    }

    private static string GetBestQualityImage(CoopProductImage coopProductImage)
    {
        return coopProductImage.Srcset
            .OrderByDescending(sourceSetPair =>
                int.Parse(sourceSetPair
                    .Last()
                    .Trim('w')))
            .First()
            .First();
    }

    public string Name => "Coop";
    public string IconUri => "/coop-logo.png";

    public string Country => "Switzerland";
}