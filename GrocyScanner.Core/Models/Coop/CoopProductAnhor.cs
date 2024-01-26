using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models.Coop;

public class CoopProductAnhor
{
    [JsonPropertyName("anchor")]
    public required string Anchor { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("json")]
    public required CoopProductTile Json { get; set; }
}