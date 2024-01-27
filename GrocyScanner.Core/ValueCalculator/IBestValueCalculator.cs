using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.ValueCalculator;

public interface IBestValueCalculator
{
    public Product? GetProductWithMostValue(IEnumerable<Product> products);
}