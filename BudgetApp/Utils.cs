using System;

namespace BudgetApp;

public static class Utils
{
    public static string MonthKey(DateTime d) => d.ToString("yyyy-MM");

    public static string NextMonth(string key) => Shift(key, 1);

    public static string PrevMonth(string key) => Shift(key, -1);

    public static (int Y, int M) Parse(string key) =>
        (int.Parse(key[..4]), int.Parse(key[5..]));

    private static string Shift(string key, int delta)
    {
        var (y, m) = Parse(key);
        return MonthKey(new DateTime(y, m, 1).AddMonths(delta));
    }
}