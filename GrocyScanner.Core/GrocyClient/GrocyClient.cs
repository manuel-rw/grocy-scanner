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

    public GrocyClient(IHttpClientFactory httpClientFactory, ILogger<GrocyClient> logger,
        IOptions<GrocyConfiguration> grocyConfiguration, IGrocyQuantityUnit grocyQuantityUnit,
        IGrocyLocations grocyLocations)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _grocyConfiguration = grocyConfiguration;
        _grocyQuantityUnit = grocyQuantityUnit;
        _grocyLocations = grocyLocations;
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
        var productBarcodes = JsonSerializer.Deserialize<IEnumerable<GrocyProductBarcode>>(content)!;
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
        _logger.LogInformation("Adding product with date {Date} and Price {Price}", bestBefore, price);
        int? existingGrocyProductId = await GetProductIdByBarcode(product.Gtin);
        if (existingGrocyProductId.HasValue)
        {
            await AddProductToStock(existingGrocyProductId.Value, amount, bestBefore, price);
            return true;
        }

        int? productId = await CreateProductAsync(product);

        if (!productId.HasValue)
        {
            return false;
        }

        await CreateProductBarcodeAsync(new GrocyProductBarcode
        {
            Barcode = product.Gtin,
            ProductId = productId.Value
        });
        await AddProductToStock(productId.Value, amount, bestBefore, price);
        return true;
    }

    public async Task AddProductToStock(int productId, int amount, DateOnly? bestBefore, double? price)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        // this is Grocy's way of "no due date". Inserting null sadly displays "today" as the due date
        DateOnly bestBeforeOrDefault = bestBefore ?? new DateOnly(2999, 12, 31);

        HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new(HttpMethod.Post,
            $"{_grocyConfiguration.Value.BaseUrl}/api/stock/products/{productId}/add");
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        httpRequestMessage.Headers.Add("accept", "application/json");
        string json = 
            $@"{{""amount"": {amount},""best_before_date"":""{bestBeforeOrDefault:yyyy-MM-dd}"",""price"":""{price:N}""}}";
        httpRequestMessage.Content = new StringContent(json);
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        await httpClient.SendAsync(httpRequestMessage);
        _logger.LogInformation("Added {ProductId} to stock with price {Price} and date {BestBeforeDate}, {Json}",
            productId, price, bestBefore, json);
    }

    private async Task<int?> CreateProductAsync(Product product)
    {
        var location = await _grocyLocations.GetLocationsAsync();
        var quantityUnits = await _grocyQuantityUnit.GetQuantityUnits();

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
            MoveOnOpen = "1",
            LocationId = $"{location.First().Id}",
            QuIdPurchase = $"{quantityUnits.First().Id}",
            QuIdStock = $"{quantityUnits.First().Id}",
            PictureFileName = pictureFileName
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