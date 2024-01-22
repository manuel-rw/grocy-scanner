using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models;

public class GrocyProductBarcode
{
    [JsonPropertyName("barcode")]
    public required string Barcode { get; set; }
    
    [JsonPropertyName("product_id")]
    public required int ProductId { get; set; }
}