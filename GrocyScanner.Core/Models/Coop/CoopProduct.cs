using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models.Coop;

public class CoopProduct
{
    [JsonPropertyName("success")]
    public required bool Success { get; set; }
    
    [JsonPropertyName("contentJsons")]
    public required CoopProductContentJson ContentJsons { get; set; }
}