using GrocyScanner.Core.Models;

namespace GrocyScanner.Core.ValueCalculator;

public class MissingImageCalculateRule : IValueCalculatorRule
{
    public int CalculateValue(Product product)
    {
        if (string.IsNullOrEmpty(product.ImageUrl))
        {
            return -5;
        }

        return 0;
    }
}