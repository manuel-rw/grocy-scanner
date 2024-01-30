using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text.Json;
using GrocyScanner.Core.Models;
using Microsoft.Extensions.Logging;

namespace GrocyScanner.Core.Providers;

public class MigrosProductProvider : IProductProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MigrosProductProvider> _logger;

    private string? _authorizationHeader;

    public MigrosProductProvider(IHttpClientFactory httpClientFactory, ILogger<MigrosProductProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Product?> GetProductByGtin(string gtin)
    {
        IReadOnlyList<int> productIds = await GetProductIds(gtin);

        if (productIds.Count != 1)
        {
            _logger.LogError("Unexpected count for gtin {Gtin}: {Count}", gtin, productIds.Count);
            return null;
        }

        return await GetProductByIdsAsync(gtin, productIds);
    }

    private async Task<Product?> GetProductByIdsAsync(string gtin, IEnumerable<int> productIds)
    {
        using HttpClient httpClient = _httpClientFactory.CreateClient();

        HttpRequestMessage httpRequestMessage =
            new(HttpMethod.Post, "https://www.migros.ch/product-display/public/v4/product-cards");
        string requestJson =
            @$"{{""offerFilter"":{{""ongoingOfferDate"":""{DateTime.Now:yyyy-MM-dd}T00:00:00"",""region"":""national"",""storeType"":""OFFLINE""}},""productFilter"":{{""uids"":[{string.Join(",", productIds)}]}}}}";
        httpRequestMessage.Content = new StringContent(
            requestJson,
            MediaTypeHeaderValue.Parse("application/json"));
        httpRequestMessage.Headers.Add("leshopch", await GetAndCreateAuthorizationHeader());
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        string content = await httpResponseMessage.Content.ReadAsStringAsync();
        try
        {
            httpResponseMessage.EnsureSuccessStatusCode();
            using JsonDocument jsonDocument = JsonDocument.Parse(content);

            if (jsonDocument.RootElement.GetArrayLength() != 1)
            {
                _logger.LogError("Migros returned more than one product for the product ids {ProductId}", productIds);
                return null;
            }

            JsonElement.ArrayEnumerator arrayEnumerator = jsonDocument.RootElement.EnumerateArray();
            arrayEnumerator.MoveNext();

            return new Product
            {
                Name = arrayEnumerator.Current.GetProperty("name").GetString()!,
                Gtin = gtin,
                Categories = ImmutableList<string>.Empty,
                ImageUrl = GetImageFromJsonDocument(arrayEnumerator.Current)
            };
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException,
                "Failed to request product details for {ProductIds} at Migros: {Content}", productIds, content);
            return null;
        }
    }

    private async ValueTask<string> GetAndCreateAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(_authorizationHeader))
        {
            return _authorizationHeader;
        }

        using HttpClient httpClient = _httpClientFactory.CreateClient();
        using HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, "https://www.migros.ch/authentication/public/v1/api/guest?authorizationNotRequired=true");
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        httpResponseMessage.EnsureSuccessStatusCode();
        IEnumerable<string>? values = httpResponseMessage.Headers.GetValues("leshopch");
        string authorizationToken = values.First();
        _authorizationHeader = authorizationToken;
        return authorizationToken;
    }

    private static string? GetImageFromJsonDocument(JsonElement jsonElement)
    {
        JsonElement imagesProperty = jsonElement.GetProperty("images");
        return imagesProperty
            .EnumerateArray()
            .Select(element =>
                element.GetProperty("url").GetString())
            .FirstOrDefault(url => !string.IsNullOrEmpty(url));
    }

    private async Task<IReadOnlyList<int>> GetProductIds(string gtin)
    {
        using HttpClient httpClient = _httpClientFactory.CreateClient();

        HttpRequestMessage httpRequestMessage =
            new(HttpMethod.Post, "https://www.migros.ch/onesearch-oc-seaapi/public/v5/search");
        httpRequestMessage.Content = new StringContent(
            @$"{{""algorithm"":""DEFAULT"",""filters"":{{}},""language"":""en"",""productIds"":[],""query"":{gtin},""regionId"":""national"",""sortFields"":[],""sortOrder"":""asc""}}",
            MediaTypeHeaderValue.Parse("application/json"));
        httpRequestMessage.Headers.Add("leshopch", await GetAndCreateAuthorizationHeader());
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        string content = await httpResponseMessage.Content.ReadAsStringAsync();
        try
        {
            httpResponseMessage.EnsureSuccessStatusCode();
            using JsonDocument jsonDocument = JsonDocument.Parse(content);
            JsonElement productIdsElement = jsonDocument.RootElement.GetProperty("productIds");
            return productIdsElement.EnumerateArray().Select(jsonElement => jsonElement.GetInt32()).ToList();
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Failed to request {Gtin} at Migros: {Content}", gtin, content);
            return ImmutableList<int>.Empty;
        }
    }

    public string Name => "Migros";
    public string IconUri => "/migros-logo.png";
    public string Country => "Switzerland";
}