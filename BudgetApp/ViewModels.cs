using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BudgetApp;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public BudgetData Data { get; }

    public ObservableCollection<MonthVM> Months { get; } = new();

    private MonthVM _current = null!;
    public MonthVM Current
    {
        get => _current;
        private set { _current = value; N(); }
    }

    private string _newItemName = "";
    public string NewItemName { get => _newItemName; set { _newItemName = value; N(); } }

    private string _newItemPlan = "";
    public string NewItemPlan { get => _newItemPlan; set { _newItemPlan = value; N(); } }

    private bool _newItemOptional;
    public bool NewItemOptional { get => _newItemOptional; set { _newItemOptional = value; N(); } }

    private string _newOneOffName = "";
    public string NewOneOffName { get => _newOneOffName; set { _newOneOffName = value; N(); } }

    private string _newOneOffAmount = "";
    public string NewOneOffAmount { get => _newOneOffAmount; set { _newOneOffAmount = value; N(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p!));

    public MainWindowViewModel()
    {
        Data = DataService.Load();
        BuildMonths();
        Current = Months.Last();
        Recalc();
    }

    private void BuildMonths()
    {
        Months.Clear();
        var key = Data.Start;
        var end = Utils.MonthKey(DateTime.Today);
        while (string.CompareOrdinal(key, end) <= 0)
        {
            Months.Add(new MonthVM(this, key));
            key = Utils.NextMonth(key);
        }
    }

    public void SelectMonth(MonthVM m)
    {
        Current = m;
        Recalc();
    }

    public void AddMonthBack()
    {
        Data.Start = Utils.PrevMonth(Data.Start);
        BuildMonths();
        SaveAndRecalc();
    }

    public void SaveAndRecalc()
    {
        DataService.Save(Data);
        Recalc();
    }

    // Пересчёт всех месяцев: цепочка переносов перерасхода необязательных статей
    public void Recalc()
    {
        var carry = new Dictionary<string, decimal>();
        foreach (var m in Months)
            m.Refresh(carry);
    }
}

public class MonthVM : INotifyPropertyChanged
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public MainWindowViewModel Root { get; }
    public string Key { get; }
    public DateTime FirstDay { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p!));

    public MonthVM(MainWindowViewModel root, string key)
    {
        Root = root;
        Key = key;
        var (y, m) = Utils.Parse(key);
        FirstDay = new DateTime(y, m, 1);
    }

    public string Title
    {
        get
        {
            var s = FirstDay.ToString("MMMM yyyy", Ru);
            return char.ToUpper(s[0]) + s[1..];
        }
    }

    public string Short => FirstDay.ToString("MMM yy", Ru);

    // Чтение без создания записи в JSON
    private MonthData D()
    {
        Root.Data.TryGetMonth(Key, out var m);
        return m ?? new MonthData();
    }

    // Запись (создаёт запись месяца при первом изменении)
    private MonthData W() => Root.Data.GetMonth(Key);

    // ---- Доход ----

    public string Income10Text
    {
        get => D().Income10.ToString("N0");
        set
        {
            if (decimal.TryParse(value, NumberStyles.Number, Ru, out var v))
            {
                W().Income10 = v;
                Root.SaveAndRecalc();
            }
        }
    }

    public string Income25Text
    {
        get => D().Income25.ToString("N0");
        set
        {
            if (decimal.TryParse(value, NumberStyles.Number, Ru, out var v))
            {
                W().Income25 = v;
                Root.SaveAndRecalc();
            }
        }
    }

    public decimal IncomeTotal => D().Income10 + D().Income25;
    public string IncomeText => $"Итог: {IncomeTotal:N0} ₽";

    // ---- Списки ----

    public List<ItemVM> RequiredItems { get; private set; } = new();
    public List<ItemVM> OptionalItems { get; private set; } = new();
    public List<OneOffVM> OneOffsVM { get; private set; } = new();
    public bool HasNoItems => RequiredItems.Count == 0 && OptionalItems.Count == 0;

    private decimal SpentOn(MonthData m, string name) =>
        m.Expenses.Where(e => e.Item == name).Sum(e => e.Amount);

    // Месяц выполнен: все обязательные статьи закрыты (необязательные не влияют)
    public bool IsDone
    {
        get
        {
            var m = D();
            var req = Root.Data.Items.Where(i => !i.Optional).ToList();
            return req.Count > 0 && req.All(i => SpentOn(m, i.Name) >= i.Plan);
        }
    }

    public bool IsCurrent => Root.Current == this;

    public void Refresh(Dictionary<string, decimal> carry)
    {
        var m = D();
        var required = new List<ItemVM>();
        var optional = new List<ItemVM>();

        foreach (var it in Root.Data.Items)
        {
            decimal spent = SpentOn(m, it.Name);
            decimal eff = it.Plan;
            decimal carryIn = 0;

            if (it.Optional)
            {
                // перерасход этого месяца перейдёт в следующий
                carryIn = carry.GetValueOrDefault(it.Name);
                eff = Math.Max(0, it.Plan - carryIn);
                carry[it.Name] = Math.Max(0, spent - eff);
            }

            var vm = new ItemVM
            {
                Name = it.Name,
                Optional = it.Optional,
                Plan = it.Plan,
                EffPlan = eff,
                Spent = spent,
                CarryIn = carryIn,
                Owner = this
            };
            vm.Expenses = m.Expenses
                .Where(e => e.Item == it.Name)
                .Select(e => new ExpenseVM { Model = e, Owner = vm })
                .ToList();

            if (it.Optional) optional.Add(vm); else required.Add(vm);
        }

        RequiredItems = required;
        OptionalItems = optional;
        OneOffsVM = m.OneOffs.Select(o => new OneOffVM { Model = o, Owner = this }).ToList();

        N(nameof(RequiredItems));
        N(nameof(OptionalItems));
        N(nameof(OneOffsVM));
        N(nameof(HasNoItems));
        N(nameof(IncomeText));
        N(nameof(FreeText));
        N(nameof(IsFreeOk));
        N(nameof(IsFreeBad));
        N(nameof(IsDone));
        N(nameof(IsCurrent));
    }

    // ---- Свободные деньги ----

    public decimal FreeMoney
    {
        get
        {
            var m = D();
            return m.Income10 + m.Income25
                 - m.Expenses.Sum(e => e.Amount)
                 - m.OneOffs.Sum(o => o.Amount);
        }
    }

    public string FreeText => $"Свободные деньги: {FreeMoney:N0} ₽";

    public bool IsFreeOk => FreeMoney >= 0;
    public bool IsFreeBad => FreeMoney < 0;
}

public class ItemVM
{
    public string Name { get; set; } = "";
    public bool Optional { get; set; }
    public decimal Plan { get; set; }
    public decimal EffPlan { get; set; }
    public decimal Spent { get; set; }
    public decimal CarryIn { get; set; }
    public MonthVM Owner { get; set; } = null!;
    public List<ExpenseVM> Expenses { get; set; } = new();
    public string AddAmountText { get; set; } = "";

    public string CounterText => $"{Spent:N0} / {EffPlan:N0}";

    // подпись «(перенос −N)», если эффективный план уменьшен переносом
    public string CarryNote => CarryIn > 0 ? $"(перенос −{CarryIn:N0})" : "";

    public bool IsOk
    {
        get
        {
            if (!Optional) return Spent >= Plan;
            return EffPlan > 0 && (double)(Spent / EffPlan) < 0.7;
        }
    }

    public bool IsWarn
    {
        get
        {
            if (!Optional) return false;
            double share = EffPlan > 0 ? (double)(Spent / EffPlan) : 1;
            return share >= 0.7 && share <= 1.0;
        }
    }

    public bool IsBad
    {
        get
        {
            if (!Optional) return Spent < Plan;
            if (EffPlan <= 0) return true;
            return (double)(Spent / EffPlan) > 1.0;
        }
    }
}

public class ExpenseVM
{
    public Expense Model { get; set; } = null!;
    public ItemVM Owner { get; set; } = null!;

    public string Text => Model.Date.Length >= 10
        ? $"{Model.Date[8..]}.{Model.Date[5..7]} — {Model.Amount:N0} ₽"
        : $"{Model.Amount:N0} ₽";
}

public class OneOffVM
{
    public OneOff Model { get; set; } = null!;
    public MonthVM Owner { get; set; } = null!;

    public string Text
    {
        get
        {
            var d = Model.Date.Length >= 10
                ? $"{Model.Date[8..]}.{Model.Date[5..7]} — "
                : "";
            return $"{d}{Model.Name} — {Model.Amount:N0} ₽";
        }
    }
}