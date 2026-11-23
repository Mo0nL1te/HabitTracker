using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BudgetApp;

public partial class ItemEditWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private readonly ItemEditVM _edit;

    public ItemEditWindow()
    {
        // для дизайнера XAML; в рантайме не используется
        InitializeComponent();
        _vm = null!;
        _edit = null!;
    }

    public ItemEditWindow(MainWindowViewModel vm, ItemEditVM edit)
    {
        InitializeComponent();
        _vm = vm;
        _edit = edit;
        DataContext = edit;
        Closing += (_, _) => _vm.SaveAndRecalc();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnRemoveExpense(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is ExpenseEditVM row)
            _edit.RemoveExpense(row);
    }

    private void OnAddExpense(object? sender, RoutedEventArgs e) => _edit.AddNew();

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        _vm.SaveAndRecalc();
        Close();
    }
}