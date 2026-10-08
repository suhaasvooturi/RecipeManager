using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using RecipeManager.Models;
using RecipeManager.Services;

namespace RecipeManager.ViewModels;

public partial class RecipesViewModel : BaseViewModel
{
    private readonly IRecipeApiService _apiService;
    private readonly IDatabaseService _databaseService;

    public ObservableCollection<Recipe> Recipes { get; } = new();
    private List<Recipe> _allLoadedRecipes = new();

    public ObservableCollection<CategoryItem> Categories { get; } = new();

    [ObservableProperty]
    private Recipe? _featuredRecipe;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _activeQuickFilter = "All";

    [ObservableProperty]
    private bool _isRefreshing;

    public RecipesViewModel(IRecipeApiService apiService, IDatabaseService databaseService)
    {
        _apiService = apiService;
        _databaseService = databaseService;
        Title = "Discover Recipes";
        InitializeCategories();
    }

    private void InitializeCategories()
    {
        Categories.Clear();
        Categories.Add(new CategoryItem { Name = "All", DisplayName = "🌟 All Recipes", Icon = "🌟", IsSelected = true });
        Categories.Add(new CategoryItem { Name = "Biryani", DisplayName = "🍚 Biryani", Icon = "🍚", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Chicken", DisplayName = "🍗 Chicken", Icon = "🍗", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Mutton", DisplayName = "🥩 Mutton", Icon = "🥩", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Vegetarian", DisplayName = "🥗 Vegetarian", Icon = "🥗", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Vegan", DisplayName = "🥑 Vegan", Icon = "🥑", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Seafood", DisplayName = "🐟 Seafood", Icon = "🐟", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Pasta", DisplayName = "🍝 Pasta", Icon = "🍝", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Dessert", DisplayName = "🍰 Dessert", Icon = "🍰", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Breakfast", DisplayName = "🍳 Breakfast", Icon = "🍳", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Starter", DisplayName = "🥣 Starters", Icon = "🥣", IsSelected = false });
        Categories.Add(new CategoryItem { Name = "Side", DisplayName = "🧀 Sides", Icon = "🧀", IsSelected = false });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (Recipes.Count > 0)
        {
            await SyncFavoriteStatusesAsync();
            return;
        }

        await LoadDataAsync();
    }

    private async Task SyncFavoriteStatusesAsync()
    {
        try
        {
            var customRecipes = await _databaseService.GetCustomRecipesAsync();
            foreach (var cr in customRecipes)
            {
                var existing = _allLoadedRecipes.FirstOrDefault(r => r.Id == $"custom_{cr.Id}");
                if (existing == null)
                {
                    var newCustom = new Recipe
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
                    _allLoadedRecipes.Insert(0, newCustom);
                }
            }

            foreach (var r in _allLoadedRecipes)
            {
                r.IsFavorite = await _databaseService.IsFavoriteAsync(r.Id);
            }

            ApplyFiltersToView();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipesViewModel] Sync error: {ex.Message}");
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = SearchAsync();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync();
        IsRefreshing = false;
    }

    private async Task LoadDataAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await _databaseService.InitializeAsync();

            // Load Featured Recipe of the Day (guaranteed non-beef)
            var random = await _apiService.GetRandomRecipeAsync();
            FeaturedRecipe = random;

            // Load Initial Recipes
            await SearchAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipesViewModel] Load failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            Recipes.Clear();
            _allLoadedRecipes.Clear();

            // 1. Check local custom recipes that match search
            var customRecipes = await _databaseService.GetCustomRecipesAsync();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var lower = SearchQuery.ToLower();
                customRecipes = customRecipes.Where(c => c.Title.ToLower().Contains(lower) || c.Category.ToLower().Contains(lower)).ToList();
            }

            foreach (var cr in customRecipes)
            {
                _allLoadedRecipes.Add(new Recipe
                {
                    Id = $"custom_{cr.Id}",
                    Title = cr.Title,
                    Category = cr.Category,
                    Area = "Personal Creation",
                    Instructions = cr.Instructions,
                    ImageUrl = cr.ImageUrl,
                    IsCustom = true,
                    IsFavorite = cr.IsFavorite
                });
            }

            // 2. Query REST API
            var query = string.IsNullOrWhiteSpace(SearchQuery) ? "chicken" : SearchQuery;
            var apiResults = await _apiService.SearchRecipesAsync(query);

            foreach (var r in apiResults)
            {
                r.IsFavorite = await _databaseService.IsFavoriteAsync(r.Id);
                _allLoadedRecipes.Add(r);
            }

            ApplyFiltersToView();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipesViewModel] Search error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task FilterByCategoryAsync(CategoryItem? catItem)
    {
        if (catItem == null) return;

        SelectedCategory = catItem.Name;
        foreach (var c in Categories)
        {
            c.IsSelected = (c.Name == catItem.Name);
        }

        IsBusy = true;
        try
        {
            Recipes.Clear();
            _allLoadedRecipes.Clear();

            if (catItem.Name == "All")
            {
                await SearchAsync();
                return;
            }

            // Include any local custom recipes in this category
            var customRecipes = await _databaseService.GetCustomRecipesAsync();
            var matchedCustom = customRecipes.Where(c => c.Category.Equals(catItem.Name, StringComparison.OrdinalIgnoreCase));
            foreach (var cr in matchedCustom)
            {
                _allLoadedRecipes.Add(new Recipe
                {
                    Id = $"custom_{cr.Id}",
                    Title = cr.Title,
                    Category = cr.Category,
                    Area = "Personal Creation",
                    Instructions = cr.Instructions,
                    ImageUrl = cr.ImageUrl,
                    IsCustom = true,
                    IsFavorite = cr.IsFavorite
                });
            }

            var apiResults = await _apiService.GetRecipesByCategoryAsync(catItem.Name);
            foreach (var r in apiResults)
            {
                r.IsFavorite = await _databaseService.IsFavoriteAsync(r.Id);
                _allLoadedRecipes.Add(r);
            }

            ApplyFiltersToView();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipesViewModel] Filter error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ApplyQuickFilter(string filter)
    {
        ActiveQuickFilter = filter;
        ApplyFiltersToView();
    }

    private void ApplyFiltersToView()
    {
        Recipes.Clear();
        IEnumerable<Recipe> filtered = _allLoadedRecipes;

        if (ActiveQuickFilter == "Quick30")
        {
            // Under 30 mins
            filtered = filtered.Where(r => r.Category.Equals("Breakfast", StringComparison.OrdinalIgnoreCase) ||
                                           r.Category.Equals("Dessert", StringComparison.OrdinalIgnoreCase) ||
                                           r.Category.Equals("Seafood", StringComparison.OrdinalIgnoreCase) ||
                                           r.Category.Equals("Pasta", StringComparison.OrdinalIgnoreCase));
        }
        else if (ActiveQuickFilter == "Favorites")
        {
            filtered = filtered.Where(r => r.IsFavorite);
        }
        else if (ActiveQuickFilter == "Custom")
        {
            filtered = filtered.Where(r => r.IsCustom);
        }

        foreach (var r in filtered)
        {
            Recipes.Add(r);
        }
    }

    [RelayCommand]
    public async Task ToggleCardFavoriteAsync(Recipe? recipe)
    {
        if (recipe == null) return;

        recipe.IsFavorite = await _databaseService.ToggleFavoriteAsync(recipe);
    }

    [RelayCommand]
    public async Task OpenSurpriseRecipeAsync()
    {
        IsBusy = true;
        try
        {
            var surprise = await _apiService.GetRandomRecipeAsync();
            if (surprise != null)
            {
                await OpenRecipeDetailAsync(surprise);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenRecipeDetailAsync(Recipe? recipe)
    {
        if (recipe == null)
            return;

        // If from category filter without full details, fetch full recipe
        if (recipe.Ingredients.Count == 0 && !recipe.IsCustom)
        {
            var fullRecipe = await _apiService.GetRecipeByIdAsync(recipe.Id);
            if (fullRecipe != null)
            {
                recipe = fullRecipe;
            }
        }

        await Shell.Current.GoToAsync("RecipeDetailPage", new Dictionary<string, object>
        {
            { "Recipe", recipe }
        });
    }

    [RelayCommand]
    public async Task NavigateToAddRecipeAsync()
    {
        await Shell.Current.GoToAsync("AddRecipePage");
    }
}
