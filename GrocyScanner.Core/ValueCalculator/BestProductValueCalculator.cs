using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.ValueCalculator;

public class BestProductValueCalculator : IBestValueCalculator
{
    private readonly IValueCalculator _valueCalculator;

    public BestProductValueCalculator(IValueCalculator valueCalculator)
    {
        _valueCalculator = valueCalculator;
    }

    public Product? GetProductWithMostValue(IEnumerable<Product> products)
    {
        return products.MaxBy(product => _valueCalculator.CalculateValue(product));
    }
}