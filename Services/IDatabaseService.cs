using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RecipeManager.Models;

namespace RecipeManager.Services;

public interface IDatabaseService
{
    Task InitializeAsync();

    // Custom Recipes
    Task<List<CustomRecipe>> GetCustomRecipesAsync();
    Task<CustomRecipe?> GetCustomRecipeByIdAsync(int id);
    Task<int> SaveCustomRecipeAsync(CustomRecipe recipe);
    Task<int> DeleteCustomRecipeAsync(CustomRecipe recipe);

    // Shopping Items
    Task<List<ShoppingItem>> GetShoppingItemsAsync();
    Task<int> SaveShoppingItemAsync(ShoppingItem item);
    Task<int> DeleteShoppingItemAsync(ShoppingItem item);
    Task ClearPurchasedShoppingItemsAsync();
    Task AddIngredientsToShoppingListAsync(IEnumerable<RecipeIngredient> ingredients, string recipeSource);

    // Meal Plan
    Task<List<MealPlanItem>> GetMealPlansAsync(DateTime? date = null);
    Task<List<MealPlanItem>> GetWeekMealPlansAsync(DateTime startDate);
    Task<int> SaveMealPlanAsync(MealPlanItem plan);
    Task<int> DeleteMealPlanAsync(MealPlanItem plan);

    // Favorites
    Task<List<FavoriteRecipe>> GetFavoritesAsync();
    Task<bool> IsFavoriteAsync(string recipeId);
    Task<bool> ToggleFavoriteAsync(Recipe recipe);
}
