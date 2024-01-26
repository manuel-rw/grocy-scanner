using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.ValueCalculator;

public interface IValueCalculator
{
    public int CalculateValue(Product product);
}