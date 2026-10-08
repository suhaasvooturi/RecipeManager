using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using RecipeManager.Models;
using RecipeManager.Services;

namespace RecipeManager.ViewModels;

public partial class AddRecipeViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private string _titleText = string.Empty;

    [ObservableProperty]
    private string _category = "Pasta";

    [ObservableProperty]
    private string _difficulty = "Medium";

    [ObservableProperty]
    private int _prepTimeMinutes = 15;

    [ObservableProperty]
    private int _cookTimeMinutes = 25;

    [ObservableProperty]
    private int _servings = 4;

    [ObservableProperty]
    private string _ingredientsText = string.Empty;

    [ObservableProperty]
    private string _instructions = string.Empty;

    [ObservableProperty]
    private string _imageUrl = "https://images.unsplash.com/photo-1621996346565-e3d5d6281691?w=500&auto=format&fit=crop&q=60";

    public ObservableCollection<string> AvailableCategories { get; } = new()
    {
        "Pasta", "Chicken", "Vegetarian", "Vegan", "Seafood", "Dessert", "Breakfast", "Soup & Stew", "Salad", "Snack"
    };

    public ObservableCollection<string> Difficulties { get; } = new()
    {
        "Easy", "Medium", "Master Chef"
    };

    public AddRecipeViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Create Custom Recipe";
    }

    [RelayCommand]
    public void SelectPresetImage(string preset)
    {
        ImageUrl = preset.ToLower() switch
        {
            "pasta" => "https://images.unsplash.com/photo-1621996346565-e3d5d6281691?w=500&auto=format&fit=crop&q=60",
            "chicken" => "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?w=500&auto=format&fit=crop&q=60",
            "salad" => "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=500&auto=format&fit=crop&q=60",
            "dessert" => "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=500&auto=format&fit=crop&q=60",
            "breakfast" => "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=500&auto=format&fit=crop&q=60",
            "seafood" => "https://images.unsplash.com/photo-1467003909585-2f8a72700288?w=500&auto=format&fit=crop&q=60",
            _ => "https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&auto=format&fit=crop&q=60"
        };
    }

    [RelayCommand]
    public async Task SaveRecipeAsync()
    {
        if (string.IsNullOrWhiteSpace(TitleText))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Please enter a recipe title.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Instructions))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Please provide preparation instructions.", "OK");
            return;
        }

        var customRecipe = new CustomRecipe
        {
            Title = TitleText.Trim(),
            Category = Category.Trim(),
            PrepTimeMinutes = PrepTimeMinutes,
            CookTimeMinutes = CookTimeMinutes,
            Servings = Servings,
            IngredientsText = IngredientsText?.Trim() ?? string.Empty,
            Instructions = Instructions.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) 
                ? "https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&auto=format&fit=crop&q=60" 
                : ImageUrl.Trim(),
            CreatedAt = DateTime.Now,
            IsFavorite = true
        };

        await _databaseService.SaveCustomRecipeAsync(customRecipe);

        await Shell.Current.DisplayAlertAsync("Saved!", $"'{TitleText}' has been saved to your CookBook!", "OK");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
