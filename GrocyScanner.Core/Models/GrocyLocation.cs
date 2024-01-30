using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models;

public class GrocyLocation
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}