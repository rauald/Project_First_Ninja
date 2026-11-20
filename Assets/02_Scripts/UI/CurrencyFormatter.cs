using System;
using System.Globalization;

public static class CurrencyFormatter
{
    public static string Format(long amount)
    {
        decimal magnitude = Math.Abs((decimal)amount);
        if (magnitude < 1_000)
        {
            return amount.ToString(CultureInfo.InvariantCulture);
        }

        var (divisor, suffix) = magnitude switch
        {
            >= 1_000_000_000 => (1_000_000_000, "c"),
            >= 1_000_000 => (1_000_000, "b"),
            _ => (1_000, "a")
        };
        decimal value = Math.Truncate((decimal)amount / divisor * 100) / 100;
        return value.ToString("0.##", CultureInfo.InvariantCulture) + suffix;
    }
}
