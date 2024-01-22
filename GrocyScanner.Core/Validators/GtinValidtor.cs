using System.Text.RegularExpressions;

namespace GrocyScanner.Core.Validators;

public class GtinValidator : IGtinValidator
{
    private static Regex _gtinRegex = new Regex("^(\\d{8}|\\d{12,14})$");
    
    public bool Validate(string barcode)
    {
        if (!_gtinRegex.IsMatch(barcode))
        {
            return false;
        }
        
        barcode = barcode.PadLeft(14, '0');
        int sum = barcode.Select((c,i) => (c - '0')  * (i % 2 == 0 ? 3 : 1)).Sum();
        return sum % 10 == 0;
    }
}