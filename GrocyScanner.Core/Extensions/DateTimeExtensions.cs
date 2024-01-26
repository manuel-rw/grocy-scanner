namespace GrocyScanner.Core.Extensions;

public static class DateTimeExtensions
{
    public static long GetUnixMilliseconds(this DateTime date)
    {
        DateTime zero = new DateTime(1970, 1, 1);
        TimeSpan span = date.Subtract(zero);

        return (long)span.TotalMilliseconds;
    }
}