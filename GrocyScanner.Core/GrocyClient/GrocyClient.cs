using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GrocyScanner.Core.Configurations;
using GrocyScanner.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrocyScanner.Core.GrocyClient;

public class GrocyClient : IGrocyClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GrocyClient> _logger;
    private readonly IOptions<GrocyConfiguration> _grocyConfiguration;
    private readonly IGrocyQuantityUnit _grocyQuantityUnit;
    private readonly IGrocyLocations _grocyLocations;
    private readonly IProductStock _productStock;

    public GrocyClient(IHttpClientFactory httpClientFactory, ILogger<GrocyClient> logger,
        IOptions<GrocyConfiguration> grocyConfiguration, IGrocyQuantityUnit grocyQuantityUnit,
        IGrocyLocations grocyLocations, IProductStock productStock)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _grocyConfiguration = grocyConfiguration;
        _grocyQuantityUnit = grocyQuantityUnit;
        _grocyLocations = grocyLocations;
        _productStock = productStock;
    }

    public async Task<int?> GetProductIdByBarcode(string gtin)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new(HttpMethod.Get,
            new Uri($"{_grocyConfiguration.Value.BaseUrl}/api/objects/product_barcodes?query%5B%5D=barcode%3D{gtin}"));
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        httpRequestMessage.Headers.Add("accept", "application/json");
        HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogDebug("Did not find a product on Grocy for gtin {Gtin}", gtin);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync();
        IEnumerable<GrocyProductBarcode>? productBarcodes = JsonSerializer.Deserialize<IEnumerable<GrocyProductBarcode>>(content)!;
        return productBarcodes.SingleOrDefault(productBarcode => productBarcode.Barcode.Equals(gtin))?.ProductId;
    }

    public async Task<Product?> GetProductByBarcode(string gtin)
    {
        int? productId = await GetProductIdByBarcode(gtin);
        if (productId == null)
        {
            return null;
        }

        using HttpClient httpClient = _httpClientFactory.CreateClient();
        using HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{_grocyConfiguration.Value.BaseUrl}/api/objects/products?query%5B%5D=id%3D{productId}");
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        httpRequestMessage.Headers.Add("accept", "application/json");
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        httpResponseMessage.EnsureSuccessStatusCode();
        string content = await httpResponseMessage.Content.ReadAsStringAsync();
        JsonDocument jsonDocument = JsonDocument.Parse(content);

        if (jsonDocument.RootElement.GetArrayLength() != 1)
        {
            _logger.LogError("Unexpected array length from Grocy for product id {ProductId}: {Length}", productId, jsonDocument.RootElement.GetArrayLength());
            return null;
        }

        JsonElement.ArrayEnumerator arrayEnumerator = jsonDocument.RootElement.EnumerateArray();
        arrayEnumerator.MoveNext();

        return new Product
        {
            Gtin = gtin,
            Name = arrayEnumerator.Current.GetProperty("name").GetString()!,
            ImageUrl = $"{_grocyConfiguration.Value.BaseUrl}/api/files/productpictures/{Base64Encode(arrayEnumerator.Current.GetProperty("picture_file_name").GetString()!)}"
        };
    }

    public async Task<bool> UpsertProduct(Product product, int amount, DateOnly? bestBefore, double? price)
    {
        _logger.LogInformation("Obtaining locations and quantity units");
        IReadOnlyList<GrocyLocation> location = await _grocyLocations.GetLocationsAsync();
        IReadOnlyList<GrocyQuantityUnit> quantityUnits = await _grocyQuantityUnit.GetQuantityUnits();

        long defaultLocationId = location.First().Id;
        long defaultQuantityUnitId = quantityUnits.First().Id;
        
        _logger.LogInformation("Location for product will be {LocationId} and quantity unit will be {QuanityUnitId}", defaultLocationId, defaultQuantityUnitId);
        _logger.LogInformation("Adding product with date {Date} and Price {Price}", bestBefore, price);
        int? existingGrocyProductId = await GetProductIdByBarcode(product.Gtin);
        if (existingGrocyProductId.HasValue)
        {
            await _productStock.AddProductToStockAsync(existingGrocyProductId.Value, amount, defaultLocationId, bestBefore, price);
            return true;
        }

        int? productId = await CreateProductAsync(product, defaultLocationId, defaultQuantityUnitId);

        if (!productId.HasValue)
        {
            return false;
        }

        await CreateProductBarcodeAsync(new GrocyProductBarcode
        {
            Barcode = product.Gtin,
            ProductId = productId.Value
        });
        await _productStock.AddProductToStockAsync(productId.Value, amount, defaultLocationId, bestBefore, price);
        return true;
    }

    private async Task<int?> CreateProductAsync(Product product, long locationId, long quantityUnitId)
    {
        string? pictureFileName = null;
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            Stream stream = await DownloadProductImageAsync(product.ImageUrl);
            pictureFileName = await UploadProductPictureAsync(product.ImageUrl, product.Gtin, stream);
        }

        GrocyProduct grocyProduct = new()
        {
            Name = product.Name,
            ShouldNotBeFrozen = "1",
            MoveOnOpen = "0",
            LocationId = $"{locationId}",
            QuIdPurchase = $"{quantityUnitId}",
            QuIdStock = $"{quantityUnitId}",
            PictureFileName = pictureFileName,
            DefaultConsumeLocationId = "0",
            Description = $"Added by grocy-scanner at {DateTime.Now.ToShortDateString()}",
            MinStockAmount = "0",
            DefaultBestBeforeDays = "0",
            ShoppingLocationId = string.Empty,
            DefaultBestBeforeDaysAfterOpen = "0",
            DefaultBestBeforeDaysAfterFreezing = "0",
            DefaultBestBeforeDaysAfterThawing = "0",
            EnableTareWeightHandling = "0",
            TareWeight = "0.0",
            NotCheckStockFulfillmentForRecipes = "0",
            Calories = "0"
        };

        string requestJson = JsonSerializer.Serialize(grocyProduct);

        HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage =
            new(HttpMethod.Post, $"{_grocyConfiguration.Value.BaseUrl}/api/objects/products");
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        httpRequestMessage.Headers.Add("accept", "application/json");
        httpRequestMessage.Content = new StringContent(requestJson);
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
        string responseAsString = await response.Content.ReadAsStringAsync();
        try
        {
            response.EnsureSuccessStatusCode();
            GrocyCreatedProductResponse grocyCreatedProductResponse =
                JsonSerializer.Deserialize<GrocyCreatedProductResponse>(responseAsString)!;
            return int.Parse(grocyCreatedProductResponse.ProductId);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Unable to read response: {ResponseJson}", responseAsString);
            return null;
        }
    }

    private async Task CreateProductBarcodeAsync(GrocyProductBarcode grocyProductBarcode)
    {
        Uri uri = new($"{_grocyConfiguration.Value.BaseUrl}/api/objects/product_barcodes");
        using HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new();
        httpRequestMessage.Method = HttpMethod.Post;
        httpRequestMessage.RequestUri = uri;
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        httpRequestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(grocyProductBarcode));
        httpRequestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return;
        }

        string content = await httpResponseMessage.Content.ReadAsStringAsync();
        GrocyErrorMessage grocyErrorMessage = JsonSerializer.Deserialize<GrocyErrorMessage>(content)!;
        _logger.LogError("Unexpected non-successful response from Grocy while creating barcode: {Error}",
            grocyErrorMessage.ErrorMessage);
        httpResponseMessage.EnsureSuccessStatusCode();
    }

    private async Task<string?> UploadProductPictureAsync(string fileUrl, string gtin, Stream stream)
    {
        string fileName = $"imported_{gtin}.png";
        string encodedFileName = Base64Encode(fileName);
        Uri uri = new($"{_grocyConfiguration.Value.BaseUrl}/api/files/productpictures/{encodedFileName}");
        using HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new(HttpMethod.Put, uri);
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);

        httpRequestMessage.Headers.Add("accept", "*/*");

        httpRequestMessage.Content = new StreamContent(stream);
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
        try
        {
            response.EnsureSuccessStatusCode();
            return fileName;
        }
        catch (HttpRequestException httpRequestException)
        {
            string contentAsString = await response.Content.ReadAsStringAsync();
            _logger.LogError(httpRequestException,
                "Failed to upload image '{FileName}' ({EncodedFileName}) ({FileUrl}): {Response}",
                fileName, encodedFileName, fileUrl, contentAsString);
            return null;
        }
    }

    private static string Base64Encode(string plainText)
    {
        byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    private async Task<Stream> DownloadProductImageAsync(string imageUrl)
    {
        Uri uri = new(imageUrl);
        using HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, uri);
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        return await httpResponseMessage.Content.ReadAsStreamAsync();
    }
}