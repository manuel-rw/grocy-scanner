using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models.Coop;

public class CoopProductTile
{
    [JsonPropertyName("elements")]
    public required List<CoopProductElement> Elements { get; set; }

    [JsonPropertyName("context")]
    public required string Context { get; set; }
}