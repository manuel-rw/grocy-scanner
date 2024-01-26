using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.ValueCalculator;

public class ProductValueCalculator : IValueCalculator
{
    private readonly IEnumerable<IValueCalculatorRule> _valueCalculatorRules;

    public ProductValueCalculator(IEnumerable<IValueCalculatorRule> valueCalculatorRules)
    {
        _valueCalculatorRules = valueCalculatorRules;
    }

    public int CalculateValue(Product product)
    {
        return _valueCalculatorRules.Sum(rule => rule.CalculateValue(product));
    }
}