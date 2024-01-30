using System.Net.Http.Headers;
using GrocyScanner.Core.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrocyScanner.Core.GrocyClient;

public class GrocyProductStock : IProductStock
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<GrocyConfiguration> _grocyConfiguration;
    private readonly ILogger<GrocyProductStock> _logger;

    public GrocyProductStock(IHttpClientFactory httpClientFactory, IOptions<GrocyConfiguration> grocyConfiguration,
        ILogger<GrocyProductStock> logger)
    {
        _httpClientFactory = httpClientFactory;
        _grocyConfiguration = grocyConfiguration;
        _logger = logger;
    }

    public async Task AddProductToStockAsync(int productId, int amount, long locationId, DateOnly? bestBefore,
        double? price)
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
            $@"{{""amount"": {amount},""best_before_date"":""{bestBeforeOrDefault:yyyy-MM-dd}"",""price"":""{price:N}"",""note"":"""",""location_id"":""{locationId}""}}";
        httpRequestMessage.Content = new StringContent(json);
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        await httpClient.SendAsync(httpRequestMessage);
        _logger.LogInformation("Added {ProductId} to stock with price {Price} and date {BestBeforeDate}, {Json}",
            productId, price, bestBefore, json);
    }

    public async Task ConsumeProduct(int productId, int amount, bool spoiled)
    {
        if (amount < 1)
        {
            throw new ArgumentException("Amount must be greater or equal than one", nameof(amount));
        }
        
        HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new(HttpMethod.Post,
            $"{_grocyConfiguration.Value.BaseUrl}/api/stock/products/{productId}/consume");
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        httpRequestMessage.Headers.Add("accept", "application/json");
        string json =
            $@"{{""amount"": {amount},""transaction_type"": ""consume"",""spoiled"":""{spoiled}""}}";
        httpRequestMessage.Content = new StringContent(json);
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        httpResponseMessage.EnsureSuccessStatusCode();
    }
}