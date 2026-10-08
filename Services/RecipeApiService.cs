using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RecipeManager.Models;

namespace RecipeManager.Services;

public class RecipeApiService : IRecipeApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.themealdb.com/api/json/v1/1/";
    private readonly JsonSerializerOptions _jsonOptions;

    public RecipeApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Recipe>> SearchRecipesAsync(string query, CancellationToken cancellationToken = default)
    {
        var localMatches = GetOfflineFallbackRecipes(query);

        try
        {
            var url = string.IsNullOrWhiteSpace(query) 
                ? "search.php?s=chicken" 
                : $"search.php?s={Uri.EscapeDataString(query.Trim())}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MealListResponse>(json, _jsonOptions);
                if (result?.Meals != null && result.Meals.Count > 0)
                {
                    var apiList = result.Meals
                        .Where(m => !IsBeef(m.StrCategory, m.StrMeal))
                        .Select(m => m.ToRecipe())
                        .ToList();

                    // Prepend any matching signature Biryani recipes
                    foreach (var local in localMatches.AsEnumerable().Reverse())
                    {
                        if (!apiList.Any(r => r.Title.Equals(local.Title, StringComparison.OrdinalIgnoreCase)))
                        {
                            apiList.Insert(0, local);
                        }
                    }

                    return apiList;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipeApiService] Search failed: {ex.Message}");
        }

        return localMatches;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("categories.php", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<CategoryListResponse>(json, _jsonOptions);
                if (result?.Categories != null && result.Categories.Count > 0)
                {
                    var list = result.Categories
                        .Where(c => !string.Equals(c.StrCategory, "Beef", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Insert Biryani and Mutton categories if not present
                    if (!list.Any(c => c.StrCategory.Equals("Biryani", StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Insert(0, new CategoryDto { StrCategory = "Biryani", StrCategoryThumb = "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=200&auto=format&fit=crop&q=80" });
                    }
                    if (!list.Any(c => c.StrCategory.Equals("Mutton", StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Insert(2, new CategoryDto { StrCategory = "Mutton", StrCategoryThumb = "https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=200&auto=format&fit=crop&q=80" });
                    }

                    return list;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipeApiService] GetCategories failed: {ex.Message}");
        }

        return GetOfflineFallbackCategories();
    }

    public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.Equals(category, "Beef", StringComparison.OrdinalIgnoreCase))
            return new List<Recipe>();

        if (string.Equals(category, "Biryani", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, "Mutton", StringComparison.OrdinalIgnoreCase))
        {
            return GetOfflineFallbackRecipes(category);
        }

        try
        {
            var response = await _httpClient.GetAsync($"filter.php?c={Uri.EscapeDataString(category)}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MealListResponse>(json, _jsonOptions);
                if (result?.Meals != null && result.Meals.Count > 0)
                {
                    var list = result.Meals
                        .Where(m => !IsBeef(category, m.StrMeal))
                        .Select(m => new Recipe
                        {
                            Id = m.IdMeal,
                            Title = m.StrMeal,
                            Category = category,
                            ImageUrl = m.StrMealThumb
                        }).ToList();

                    if (string.Equals(category, "Chicken", StringComparison.OrdinalIgnoreCase))
                    {
                        var chickenBiryani = GetOfflineFallbackRecipes("Chicken Biryani").FirstOrDefault();
                        if (chickenBiryani != null && !list.Any(r => r.Title.Equals(chickenBiryani.Title, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Insert(0, chickenBiryani);
                        }
                    }

                    return list;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipeApiService] GetByCategory failed: {ex.Message}");
        }

        return GetOfflineFallbackRecipes(category);
    }

    public async Task<Recipe?> GetRecipeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (id == "90001" || id == "90002")
        {
            return GetOfflineFallbackRecipes("").FirstOrDefault(r => r.Id == id);
        }

        try
        {
            var response = await _httpClient.GetAsync($"lookup.php?i={Uri.EscapeDataString(id)}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MealListResponse>(json, _jsonOptions);
                var meal = result?.Meals?.FirstOrDefault();
                if (meal != null && !IsBeef(meal.StrCategory, meal.StrMeal))
                {
                    return meal.ToRecipe();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipeApiService] GetRecipeById failed: {ex.Message}");
        }

        return GetOfflineFallbackRecipes("").FirstOrDefault(r => r.Id == id);
    }

    public async Task<Recipe?> GetRandomRecipeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var response = await _httpClient.GetAsync("random.php", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<MealListResponse>(json, _jsonOptions);
                    var meal = result?.Meals?.FirstOrDefault();
                    if (meal != null && !IsBeef(meal.StrCategory, meal.StrMeal))
                    {
                        return meal.ToRecipe();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipeApiService] GetRandomRecipe failed: {ex.Message}");
        }

        return GetOfflineFallbackRecipes("").FirstOrDefault();
    }

    private static bool IsBeef(string? category, string? title)
    {
        if (!string.IsNullOrEmpty(category) && category.Contains("Beef", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrEmpty(title) && title.Contains("Beef", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private List<Recipe> GetOfflineFallbackRecipes(string filter)
    {
        var list = new List<Recipe>
        {
            new Recipe
            {
                Id = "90001",
                Title = "Royal Hyderabadi Dum Chicken Biryani",
                Category = "Chicken",
                Area = "Indian",
                ImageUrl = "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=800&auto=format&fit=crop&q=80",
                Instructions = "1. Marination: In a deep mixing bowl, combine chicken with yogurt, ginger-garlic paste, kashmiri red chili powder, turmeric, royal biryani masala, half of the fried onions (birista), chopped mint, chopped coriander, slit green chilies, fresh lemon juice, 2 tbsp oil, and salt. Massage thoroughly and marinate for 1 to 2 hours.\n2. Par-cooking the Rice: In a large stockpot, boil 3 liters of water with whole spices (bay leaf, green cardamom, cloves, cinnamon stick), 2 tbsp salt, and 1 tsp ghee. Add washed and soaked long-grain basmati rice. Cook for 5-6 minutes until 70% done. Drain immediately.\n3. Dum Layering: In a heavy-bottomed pot, spread the marinated raw chicken evenly at the base. Layer the steaming par-cooked basmati rice gently on top.\n4. Aromatics: Drizzle saffron milk, pure desi ghee, and kewra water. Scatter remaining crispy golden onions, chopped mint, and fresh coriander over top.\n5. Dum Cooking: Seal pot rim tightly with aluminum foil and press heavy lid. Cook on medium-high heat for 10 minutes to generate internal steam, then place on a heavy tawa on low flame for 35 minutes.\n6. Rest & Serve: Rest 15 minutes before opening. Gently fluff from the edges to reveal fragrant saffron and spiced grains. Serve hot with cucumber raita and mirchi ka salan.",
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Name = "Fresh Chicken (bone-in pieces)", Measure = "800g" },
                    new() { Name = "Aged Long-Grain Basmati Rice", Measure = "500g" },
                    new() { Name = "Thick Plain Yogurt / Curd", Measure = "200g" },
                    new() { Name = "Golden Fried Onions (Birista)", Measure = "1.5 cups" },
                    new() { Name = "Ginger-Garlic Paste", Measure = "2 tbsp" },
                    new() { Name = "Fresh Mint Leaves", Measure = "1 cup chopped" },
                    new() { Name = "Fresh Coriander Leaves", Measure = "1 cup chopped" },
                    new() { Name = "Kashmiri Red Chili Powder", Measure = "1.5 tbsp" },
                    new() { Name = "Royal Biryani Masala", Measure = "1.5 tbsp" },
                    new() { Name = "Warm Saffron Milk", Measure = "4 tbsp" },
                    new() { Name = "Pure Desi Ghee", Measure = "4 tbsp" },
                    new() { Name = "Green Chilies", Measure = "4 slit" }
                }
            },
            new Recipe
            {
                Id = "90002",
                Title = "Nawabi Shahi Mutton Dum Biryani",
                Category = "Mutton",
                Area = "Indian",
                ImageUrl = "https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=800&auto=format&fit=crop&q=80",
                Instructions = "1. Tenderization & Marination: Clean mutton pieces thoroughly. In a large bowl, mix mutton with raw papaya paste, ginger-garlic paste, yogurt, half the fried onions, chili powder, nawabi biryani masala, mint, coriander, green chilies, lemon juice, 2 tbsp ghee, and salt. Marinate in refrigerator for at least 4 hours (overnight recommended for melt-in-mouth tenderness).\n2. Cooking Basmati: Boil water with whole spices and salt. Add drained soaked basmati rice and cook until 70% done (grain breaks into three parts). Drain thoroughly.\n3. Dum Assembly: In a heavy cast iron or copper biryani pot, grease with 2 tbsp ghee and spread the marinated mutton pieces in a single layer at the base.\n4. Rice & Aromatic Layering: Spread the steaming drained basmati rice evenly over the mutton. Drizzle saffron milk, remaining desi ghee, and scatter crispy golden onions and freshly chopped mint.\n5. Slow Dum Infusion: Seal the pot securely with heavy-duty foil and a tight lid. Cook on high flame for 12 minutes to build deep steam pressure, then transfer to a low flame on a hot skillet for 55 minutes until mutton is juicy and tender.\n6. Rest & Serve: Rest for 15 minutes before unsealing. Gently mix with a flat spoon from bottom to top to preserve elongated rice grains. Serve with mirchi ka salan, spiced onion rings, and mint raita.",
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Name = "Tender Goat Mutton / Lamb", Measure = "800g" },
                    new() { Name = "Aged Royal Basmati Rice", Measure = "500g" },
                    new() { Name = "Plain Whisked Yogurt", Measure = "1 cup" },
                    new() { Name = "Crispy Fried Onions (Birista)", Measure = "2 cups" },
                    new() { Name = "Ginger-Garlic Paste", Measure = "2.5 tbsp" },
                    new() { Name = "Raw Papaya Paste", Measure = "1 tbsp" },
                    new() { Name = "Nawabi Biryani Garam Masala", Measure = "2 tbsp" },
                    new() { Name = "Kashmiri Chili Powder", Measure = "2 tbsp" },
                    new() { Name = "Fresh Mint & Coriander", Measure = "1.5 cups" },
                    new() { Name = "Warm Saffron Milk", Measure = "1/4 cup" },
                    new() { Name = "Desi Ghee", Measure = "5 tbsp" },
                    new() { Name = "Green Chilies", Measure = "5 slit" }
                }
            },
            new Recipe
            {
                Id = "52772",
                Title = "Teriyaki Chicken Casserole",
                Category = "Chicken",
                Area = "Japanese",
                ImageUrl = "https://www.themealdb.com/images/media/meals/wvpsxx1468256321.jpg",
                Instructions = "1. Preheat oven to 350°F (175°C).\n2. Stir soy sauce, water, and brown sugar together in a small saucepan over medium heat until sugar is dissolved.\n3. Combine cooked rice, chicken pieces, broccoli, and peas in a large casserole dish.\n4. Pour sauce over the chicken mixture and toss lightly to coat.\n5. Bake in preheated oven for 30 minutes until bubbling hot.",
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Name = "Chicken Breast", Measure = "3/4 lb" },
                    new() { Name = "Soy Sauce", Measure = "1/2 cup" },
                    new() { Name = "Brown Sugar", Measure = "1/4 cup" },
                    new() { Name = "Garlic", Measure = "2 cloves minced" },
                    new() { Name = "White Rice", Measure = "2 cups cooked" },
                    new() { Name = "Broccoli Florets", Measure = "1 cup" }
                }
            },
            new Recipe
            {
                Id = "52977",
                Title = "Corba (Lentil Soup)",
                Category = "Vegetarian",
                Area = "Turkish",
                ImageUrl = "https://www.themealdb.com/images/media/meals/58oia91564916529.jpg",
                Instructions = "1. Pick through lentils and rinse under cold water.\n2. In a large pot, melt butter and saute diced onions until translucent.\n3. Add tomato paste, cumin, and mint; stir for 1 minute.\n4. Add lentils and vegetable stock. Bring to a boil, then reduce heat and simmer for 25 minutes.\n5. Puree soup with an immersion blender until velvety smooth. Serve hot with lemon wedges.",
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Name = "Red Lentils", Measure = "1 cup" },
                    new() { Name = "Onion", Measure = "1 finely chopped" },
                    new() { Name = "Carrot", Measure = "1 diced" },
                    new() { Name = "Tomato Paste", Measure = "1 tbsp" },
                    new() { Name = "Vegetable Broth", Measure = "4 cups" },
                    new() { Name = "Cumin", Measure = "1 tsp" }
                }
            },
            new Recipe
            {
                Id = "52819",
                Title = "Cajun Spiced Salmon",
                Category = "Seafood",
                Area = "American",
                ImageUrl = "https://www.themealdb.com/images/media/meals/450zjf1587342790.jpg",
                Instructions = "1. In a small bowl, combine paprika, garlic powder, onion powder, cayenne pepper, thyme, oregano, salt, and black pepper.\n2. Brush fresh salmon fillets with olive oil on both sides, then generously coat with spice rub.\n3. Heat a cast-iron skillet over high heat with oil.\n4. Sear salmon skin-side down for 4 minutes, flip and cook 3-4 minutes until cooked through.",
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Name = "Salmon Fillets", Measure = "2 fillets" },
                    new() { Name = "Cajun Seasoning", Measure = "2 tbsp" },
                    new() { Name = "Olive Oil", Measure = "2 tbsp" },
                    new() { Name = "Lemon", Measure = "1 sliced" }
                }
            },
            new Recipe
            {
                Id = "52855",
                Title = "Banana Pancakes",
                Category = "Dessert",
                Area = "American",
                ImageUrl = "https://www.themealdb.com/images/media/meals/sywswr1511383814.jpg",
                Instructions = "1. Mash ripe bananas in a bowl.\n2. Whisk in eggs, melted butter, milk, and vanilla extract.\n3. Fold in flour, baking powder, cinnamon, and a pinch of salt.\n4. Heat a lightly greased griddle over medium heat.\n5. Pour 1/4 cup batter for each pancake. Cook until bubbles form, flip and cook until golden brown.",
                Ingredients = new List<RecipeIngredient>
                {
                    new() { Name = "Ripe Bananas", Measure = "2" },
                    new() { Name = "Flour", Measure = "1 cup" },
                    new() { Name = "Eggs", Measure = "2" },
                    new() { Name = "Milk", Measure = "1/2 cup" },
                    new() { Name = "Baking Powder", Measure = "1 tsp" },
                    new() { Name = "Maple Syrup", Measure = "for serving" }
                }
            }
        };

        if (string.IsNullOrWhiteSpace(filter))
            return list;

        var lower = filter.ToLower();
        return list.Where(r => r.Title.ToLower().Contains(lower) || 
                               r.Category.ToLower().Contains(lower) || 
                               (r.Area != null && r.Area.ToLower().Contains(lower))).ToList();
    }

    private List<CategoryDto> GetOfflineFallbackCategories()
    {
        return new List<CategoryDto>
        {
            new() { StrCategory = "Biryani", StrCategoryThumb = "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=200&auto=format&fit=crop&q=80" },
            new() { StrCategory = "Chicken", StrCategoryThumb = "https://www.themealdb.com/images/category/chicken.png" },
            new() { StrCategory = "Mutton", StrCategoryThumb = "https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=200&auto=format&fit=crop&q=80" },
            new() { StrCategory = "Vegetarian", StrCategoryThumb = "https://www.themealdb.com/images/category/vegetarian.png" },
            new() { StrCategory = "Vegan", StrCategoryThumb = "https://www.themealdb.com/images/category/vegan.png" },
            new() { StrCategory = "Seafood", StrCategoryThumb = "https://www.themealdb.com/images/category/seafood.png" },
            new() { StrCategory = "Pasta", StrCategoryThumb = "https://www.themealdb.com/images/category/pasta.png" },
            new() { StrCategory = "Dessert", StrCategoryThumb = "https://www.themealdb.com/images/category/dessert.png" },
            new() { StrCategory = "Breakfast", StrCategoryThumb = "https://www.themealdb.com/images/category/breakfast.png" },
            new() { StrCategory = "Starter", StrCategoryThumb = "https://www.themealdb.com/images/category/starter.png" },
            new() { StrCategory = "Side", StrCategoryThumb = "https://www.themealdb.com/images/category/side.png" }
        };
    }
}
