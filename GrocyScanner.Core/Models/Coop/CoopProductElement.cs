using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models.Coop;

public class CoopProductElement
{
    [JsonPropertyName("elementType")]
    public required string ElementType { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("href")]
    public required string Href { get; set; }

    [JsonPropertyName("image")]
    public required CoopProductImage Image { get; set; }

    [JsonPropertyName("quantity")]
    public required string Quantity { get; set; }

    [JsonPropertyName("ratingAmount")]
    public required int RatingAmount { get; set; }

    [JsonPropertyName("ratingValue")]
    public required double RatingValue { get; set; }

    [JsonPropertyName("ratingLink")]
    public required string RatingLink { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("price")]
    public required string Price { get; set; }

    [JsonPropertyName("udoCat")]
    public required List<string> UdoCat { get; set; }
}