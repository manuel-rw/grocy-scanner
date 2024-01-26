using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models.Coop;

public class CoopProductContentJson
{
    [JsonPropertyName("anchors")]
    public required List<CoopProductAnhor> Anchors { get; set; }
}