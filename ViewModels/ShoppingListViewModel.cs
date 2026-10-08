using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using RecipeManager.Models;
using RecipeManager.Services;

namespace RecipeManager.ViewModels;

public partial class ShoppingListViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    public ObservableCollection<ShoppingItem> Items { get; } = new();
    private List<ShoppingItem> _allItems = new();

    [ObservableProperty]
    private string _newItemName = string.Empty;

    [ObservableProperty]
    private string _newItemMeasure = string.Empty;

    [ObservableProperty]
    private string _summaryText = "0 items remaining";

    [ObservableProperty]
    private double _completionRatio = 0.0;

    [ObservableProperty]
    private string _progressPercentageText = "0%";

    [ObservableProperty]
    private string _selectedCategoryFilter = "All";

    public ShoppingListViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Shopping List";
    }

    [RelayCommand]
    public async Task LoadItemsAsync()
    {
        IsBusy = true;
        try
        {
            var dbItems = await _databaseService.GetShoppingItemsAsync();
            _allItems = dbItems;
            ApplyFilter();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShoppingListViewModel] Load error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemName))
        {
            await Shell.Current.DisplayAlertAsync("Shopping List", "Please enter an item name.", "OK");
            return;
        }

        var item = new ShoppingItem
        {
            Name = NewItemName.Trim(),
            Measure = NewItemMeasure?.Trim() ?? string.Empty,
            Category = "Manual Entry",
            IsPurchased = false,
            RecipeSource = "My Shopping List",
            CreatedAt = DateTime.Now
        };

        await _databaseService.SaveShoppingItemAsync(item);
        _allItems.Insert(0, item);
        ApplyFilter();

        NewItemName = string.Empty;
        NewItemMeasure = string.Empty;
        UpdateSummary();
    }

    [RelayCommand]
    public async Task QuickAddStapleAsync(string stapleName)
    {
        if (string.IsNullOrWhiteSpace(stapleName)) return;

        string measure = stapleName switch
        {
            "Milk" => "1 Carton",
            "Eggs" => "1 Dozen",
            "Bread" => "1 Loaf",
            "Olive Oil" => "1 Bottle",
            "Garlic" => "3 Cloves",
            "Tomatoes" => "500g",
            "Parmesan" => "1 Wedge (150g)",
            "Lemon" => "2 pcs",
            _ => "1 pack"
        };

        var item = new ShoppingItem
        {
            Name = stapleName,
            Measure = measure,
            Category = "Pantry Staple",
            IsPurchased = false,
            RecipeSource = "Quick Staple",
            CreatedAt = DateTime.Now
        };

        await _databaseService.SaveShoppingItemAsync(item);
        _allItems.Insert(0, item);
        ApplyFilter();
        UpdateSummary();
    }

    [RelayCommand]
    public void FilterCategory(string category)
    {
        SelectedCategoryFilter = category;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();
        var list = SelectedCategoryFilter == "All"
            ? _allItems
            : _allItems.Where(i => i.Category.Equals(SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var it in list)
        {
            Items.Add(it);
        }
    }

    [RelayCommand]
    public async Task TogglePurchasedAsync(ShoppingItem? item)
    {
        if (item == null) return;

        item.IsPurchased = !item.IsPurchased;
        await _databaseService.SaveShoppingItemAsync(item);
        UpdateSummary();
    }

    [RelayCommand]
    public async Task DeleteItemAsync(ShoppingItem? item)
    {
        if (item == null) return;

        await _databaseService.DeleteShoppingItemAsync(item);
        _allItems.Remove(item);
        Items.Remove(item);
        UpdateSummary();
    }

    [RelayCommand]
    public async Task ClearPurchasedAsync()
    {
        var count = _allItems.Count(i => i.IsPurchased);
        if (count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Shopping List", "No purchased items to clear.", "OK");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlertAsync("Clear Completed", $"Remove {count} purchased item(s)?", "Yes", "No");
        if (!confirm) return;

        await _databaseService.ClearPurchasedShoppingItemsAsync();
        _allItems.RemoveAll(i => i.IsPurchased);
        ApplyFilter();
        UpdateSummary();
    }

    [RelayCommand]
    public async Task ClearAllItemsAsync()
    {
        if (_allItems.Count == 0) return;

        bool confirm = await Shell.Current.DisplayAlertAsync("Clear Entire List", "Are you sure you want to delete all grocery items?", "Clear All", "Cancel");
        if (!confirm) return;

        foreach (var it in _allItems.ToList())
        {
            await _databaseService.DeleteShoppingItemAsync(it);
        }
        _allItems.Clear();
        Items.Clear();
        UpdateSummary();
    }

    [RelayCommand]
    public async Task ShareListAsync()
    {
        if (_allItems.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Shopping List", "Your shopping list is empty.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("🛒 Recipe Manager - My Shopping List:");
        sb.AppendLine("------------------------------------");

        foreach (var item in _allItems.OrderBy(i => i.IsPurchased))
        {
            var status = item.IsPurchased ? "[✓]" : "[ ]";
            sb.AppendLine($"{status} {item.DisplayText}");
        }

        var text = sb.ToString();
        await Clipboard.Default.SetTextAsync(text);
        await Shell.Current.DisplayAlertAsync("Copied!", "Shopping list copied to clipboard. Ready to paste into messaging or notes!", "OK");
    }

    private void UpdateSummary()
    {
        var total = _allItems.Count;
        var remaining = _allItems.Count(i => !i.IsPurchased);
        var completed = _allItems.Count(i => i.IsPurchased);
        SummaryText = $"{remaining} to buy • {completed} purchased";

        if (total > 0)
        {
            CompletionRatio = (double)completed / total;
            ProgressPercentageText = $"{(int)(CompletionRatio * 100)}% completed";
        }
        else
        {
            CompletionRatio = 0.0;
            ProgressPercentageText = "0% completed";
        }
    }
}
