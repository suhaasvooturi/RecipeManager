using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using RecipeManager.Models;
using RecipeManager.Services;

namespace RecipeManager.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IRecipeApiService _apiService;

    public ObservableCollection<FavoriteRecipe> FavoriteRecipes { get; } = new();
    public ObservableCollection<CustomRecipe> CustomRecipes { get; } = new();

    [ObservableProperty]
    private bool _hasNoFavorites;

    [ObservableProperty]
    private bool _hasNoCustom;

    public FavoritesViewModel(IDatabaseService databaseService, IRecipeApiService apiService)
    {
        _databaseService = databaseService;
        _apiService = apiService;
        Title = "My CookBook";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            FavoriteRecipes.Clear();
            var favs = await _databaseService.GetFavoritesAsync();
            foreach (var f in favs)
            {
                FavoriteRecipes.Add(f);
            }
            HasNoFavorites = FavoriteRecipes.Count == 0;

            CustomRecipes.Clear();
            var customs = await _databaseService.GetCustomRecipesAsync();
            foreach (var c in customs)
            {
                CustomRecipes.Add(c);
            }
            HasNoCustom = CustomRecipes.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesViewModel] Load error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenFavoriteRecipeAsync(FavoriteRecipe? fav)
    {
        if (fav == null) return;

        IsBusy = true;
        try
        {
            var recipe = await _apiService.GetRecipeByIdAsync(fav.RecipeId);
            if (recipe == null)
            {
                recipe = new Recipe
                {
                    Id = fav.RecipeId,
                    Title = fav.Title,
                    Category = fav.Category,
                    Area = fav.Area,
                    ImageUrl = fav.ImageUrl,
                    IsFavorite = true
                };
            }
            recipe.IsFavorite = true;

            await Shell.Current.GoToAsync("RecipeDetailPage", new System.Collections.Generic.Dictionary<string, object>
            {
                { "Recipe", recipe }
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenCustomRecipeAsync(CustomRecipe? cr)
    {
        if (cr == null) return;

        var recipe = new Recipe
        {
            Id = $"custom_{cr.Id}",
            Title = cr.Title,
            Category = cr.Category,
            Area = "Personal Creation",
            Instructions = cr.Instructions,
            ImageUrl = cr.ImageUrl,
            IsCustom = true,
            IsFavorite = cr.IsFavorite
        };

        if (!string.IsNullOrWhiteSpace(cr.IngredientsText))
        {
            var lines = cr.IngredientsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                recipe.Ingredients.Add(new RecipeIngredient { Name = line.Trim() });
            }
        }

        await Shell.Current.GoToAsync("RecipeDetailPage", new System.Collections.Generic.Dictionary<string, object>
        {
            { "Recipe", recipe }
        });
    }

    [RelayCommand]
    public async Task DeleteCustomRecipeAsync(CustomRecipe? cr)
    {
        if (cr == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync("Delete Recipe", $"Permanently delete '{cr.Title}'?", "Delete", "Cancel");
        if (!confirm) return;

        await _databaseService.DeleteCustomRecipeAsync(cr);
        CustomRecipes.Remove(cr);
        HasNoCustom = CustomRecipes.Count == 0;
    }

    [RelayCommand]
    public async Task NavigateToAddRecipeAsync()
    {
        await Shell.Current.GoToAsync("AddRecipePage");
    }
}
