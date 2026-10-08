using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecipeManager.Models;

namespace RecipeManager.Services;

public interface IRecipeApiService
{
    Task<List<Recipe>> SearchRecipesAsync(string query, CancellationToken cancellationToken = default);
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<Recipe>> GetRecipesByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<Recipe?> GetRecipeByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Recipe?> GetRandomRecipeAsync(CancellationToken cancellationToken = default);
}
