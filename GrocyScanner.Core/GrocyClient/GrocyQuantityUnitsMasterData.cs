using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using GrocyScanner.Core.Configurations;
using GrocyScanner.Core.Models;
using Microsoft.Extensions.Options;

namespace GrocyScanner.Core.GrocyClient;

public class GrocyQuantityUnitsMasterData : IGrocyQuantityUnit
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<GrocyConfiguration> _grocyConfiguration;

    public GrocyQuantityUnitsMasterData(IHttpClientFactory httpClientFactory, IOptions<GrocyConfiguration> grocyConfiguration)
    {
        _httpClientFactory = httpClientFactory;
        _grocyConfiguration = grocyConfiguration;
    }

    public async Task<IReadOnlyList<GrocyQuantityUnit>> GetQuantityUnits()
    {
        using HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpRequestMessage httpRequestMessage = new(HttpMethod.Get,
            new Uri($"{_grocyConfiguration.Value.BaseUrl}/api/objects/quantity_units"));
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        httpRequestMessage.Headers.Add("GROCY-API-KEY", _grocyConfiguration.Value.ApiKey);
        HttpResponseMessage responseMessage = await httpClient.SendAsync(httpRequestMessage);
        string content = await responseMessage.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IReadOnlyList<GrocyQuantityUnit>>(content)!;
    }
}