using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models;

public class GrocyCreatedProductResponse
{
    [JsonPropertyName("created_object_id")]
    public required string ProductId { get; set; }
}