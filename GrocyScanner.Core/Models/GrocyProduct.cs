using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models;

public class GrocyProduct
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    [JsonPropertyName("qu_id_purchase")]
    public string? QuIdPurchase { get; set; }

    [JsonPropertyName("qu_id_stock")]
    public string? QuIdStock { get; set; }

    [JsonPropertyName("min_stock_amount")]
    public string? MinStockAmount { get; set; }

    [JsonPropertyName("default_best_before_days")]
    public string? DefaultBestBeforeDays { get; set; }

    [JsonPropertyName("product_group_id")]
    public string? ProductGroupId { get; set; }

    [JsonPropertyName("picture_file_name")]
    public string? PictureFileName { get; set; }

    [JsonPropertyName("default_best_before_days_after_open")]
    public string? DefaultBestBeforeDaysAfterOpen { get; set; }

    [JsonPropertyName("enable_tare_weight_handling")]
    public string? EnableTareWeightHandling { get; set; }

    [JsonPropertyName("not_check_stock_fulfillment_for_recipes")]
    public string? NotCheckStockFulfillmentForRecipes { get; set; }

    [JsonPropertyName("should_not_be_frozen")]
    public required string ShouldNotBeFrozen { get; set; }

    [JsonPropertyName("default_consume_location_id")]
    public string? DefaultConsumeLocationId { get; set; }

    [JsonPropertyName("move_on_open")]
    public required string MoveOnOpen { get; set; }
}