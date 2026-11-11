using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BudgetApp;

public class Expense
{
    [JsonPropertyName("item")] public string Item { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("date")] public string Date { get; set; } = "";
}

public class OneOff
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("date")] public string Date { get; set; } = "";
}

public class Item
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("plan")] public decimal Plan { get; set; }
    [JsonPropertyName("optional")] public bool Optional { get; set; }
}

public class MonthData
{
    [JsonPropertyName("income_10")] public decimal Income10 { get; set; }
    [JsonPropertyName("income_25")] public decimal Income25 { get; set; }
    [JsonPropertyName("expenses")] public List<Expense> Expenses { get; set; } = new();
    [JsonPropertyName("oneoff")] public List<OneOff> OneOffs { get; set; } = new();
}

public class BudgetData
{
    [JsonPropertyName("created")] public string Created { get; set; } = "";
    [JsonPropertyName("start")] public string Start { get; set; } = "";
    [JsonPropertyName("items")] public List<Item> Items { get; set; } = new();
    [JsonPropertyName("months")] public Dictionary<string, MonthData> Months { get; set; } = new();

    public MonthData GetMonth(string key)
    {
        if (!Months.TryGetValue(key, out var m))
        {
            m = new MonthData();
            Months[key] = m;
        }
        return m;
    }

    public bool TryGetMonth(string key, out MonthData? m)
    {
        m = null;
        return Months.TryGetValue(key, out m);
    }
}