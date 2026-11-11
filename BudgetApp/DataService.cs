using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BudgetApp;

public static class DataService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public static string DataDir { get; }
    public static string DataFile { get; }

    static DataService()
    {
        // DataHandler — рядом с файлами проекта (ищем .csproj вверх от exe)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.EnumerateFiles("*.csproj").Any())
            dir = dir.Parent;
        var root = dir != null ? dir.FullName : AppContext.BaseDirectory;
        DataDir = Path.Combine(root, "DataHandler");
        Directory.CreateDirectory(DataDir);
        DataFile = Path.Combine(DataDir, "budget.json");
    }

    public static BudgetData Load()
    {
        if (File.Exists(DataFile))
        {
            try
            {
                var data = JsonSerializer.Deserialize<BudgetData>(File.ReadAllText(DataFile), JsonOpts);
                if (data != null) return data;
            }
            catch (JsonException) { /* повреждённый файл — создаём новый */ }
        }
        return NewBudget();
    }

    public static void Save(BudgetData data) =>
        File.WriteAllText(DataFile, JsonSerializer.Serialize(data, JsonOpts));

    private static BudgetData NewBudget() => new()
    {
        Created = DateTime.Today.ToString("yyyy-MM-dd"),
        Start = Utils.MonthKey(DateTime.Today),
        Items = new List<Item>(),
        Months = new Dictionary<string, MonthData>()
    };
}