using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.ValueCalculator;

public interface IValueCalculatorRule
{
    public int CalculateValue(Product product);
}