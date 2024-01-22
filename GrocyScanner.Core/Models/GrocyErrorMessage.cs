using System.Text.Json.Serialization;

namespace GrocyScanner.Core.Models;

public class GrocyErrorMessage
{
    [JsonPropertyName("error_message")]
    public required string ErrorMessage { get; set; }
}