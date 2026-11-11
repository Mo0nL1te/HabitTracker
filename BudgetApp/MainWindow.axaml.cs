using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BudgetApp;

public partial class MainWindow : Window
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private MainWindowViewModel VM => (MainWindowViewModel)DataContext!;

    private void OnMonthClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is MonthVM m)
            VM.SelectMonth(m);
    }

    private void OnAddMonthBack(object? sender, RoutedEventArgs e) => VM.AddMonthBack();

    private void OnAddExpense(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not ItemVM ivm) return;
        if (!decimal.TryParse(ivm.AddAmountText, NumberStyles.Number, Ru, out var amt) || amt <= 0)
            return;
        VM.Data.GetMonth(ivm.Owner.Key).Expenses.Add(new Expense
        {
            Item = ivm.Name,
            Amount = amt,
            Date = DateTime.Today.ToString("yyyy-MM-dd")
        });
        VM.SaveAndRecalc();
    }

    private void OnRemoveExpense(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not ExpenseVM evm) return;
        var month = evm.Owner.Owner;
        VM.Data.GetMonth(month.Key).Expenses.Remove(evm.Model);
        VM.SaveAndRecalc();
    }

    private void OnAddItem(object? sender, RoutedEventArgs e)
    {
        var name = VM.NewItemName.Trim();
        if (name.Length == 0) return;
        if (!decimal.TryParse(VM.NewItemPlan, NumberStyles.Number, Ru, out var plan)) return;
        if (VM.Data.Items.Any(i => i.Name == name)) return;

        VM.Data.Items.Add(new Item
        {
            Name = name,
            Plan = plan,
            Optional = VM.NewItemOptional
        });
        VM.NewItemName = "";
        VM.NewItemPlan = "";
        VM.NewItemOptional = false;
        VM.SaveAndRecalc();
    }

    private void OnAddOneOff(object? sender, RoutedEventArgs e)
    {
        var name = VM.NewOneOffName.Trim();
        if (name.Length == 0) return;
        if (!decimal.TryParse(VM.NewOneOffAmount, NumberStyles.Number, Ru, out var amt) || amt <= 0)
            return;

        VM.Data.GetMonth(VM.Current.Key).OneOffs.Add(new OneOff
        {
            Name = name,
            Amount = amt,
            Date = DateTime.Today.ToString("yyyy-MM-dd")
        });
        VM.NewOneOffName = "";
        VM.NewOneOffAmount = "";
        VM.SaveAndRecalc();
    }

    private void OnRemoveOneOff(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not OneOffVM ovm) return;
        VM.Data.GetMonth(ovm.Owner.Key).OneOffs.Remove(ovm.Model);
        VM.SaveAndRecalc();
    }
}