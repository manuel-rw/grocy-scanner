using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models;

public class GrocyQuantityUnit
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("name_plural")]
    public required string NamePlural { get; set; }
}