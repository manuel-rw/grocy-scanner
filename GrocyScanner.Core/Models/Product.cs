namespace GrocyScanner.Core.Models;

public class Product
{
    public required string Gtin { get; set; }
    
    public required string Name { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public IEnumerable<string>? Categories { get; set; }
}