namespace GrocyScanner.Core.Validators;

public interface IGtinValidator
{
    public bool Validate(string barcode);
}