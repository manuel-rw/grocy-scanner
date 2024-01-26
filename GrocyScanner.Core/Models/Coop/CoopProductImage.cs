using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models.Coop;

public class CoopProductImage
{
    [JsonPropertyName("lazyload")]
    public required bool Lazyload { get; set; }

    [JsonPropertyName("lazyloadSrc")]
    public required string LazyloadSrc { get; set; }

    [JsonPropertyName("loader")]
    public required string Loader { get; set; }

    [JsonPropertyName("sizes")]
    public required Dictionary<string, string> Sizes { get; set; }

    [JsonPropertyName("src")]
    public required string Src { get; set; }

    [JsonPropertyName("srcset")]
    public required List<List<string>> Srcset { get; set; }
}