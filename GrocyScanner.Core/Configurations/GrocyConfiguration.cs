namespace GrocyScanner.Core.Configurations;

public class GrocyConfiguration
{
    public const string Name = "Grocy";
    
    public required string BaseUrl { get; set; }
    
    public required string ApiKey { get; set; }
}