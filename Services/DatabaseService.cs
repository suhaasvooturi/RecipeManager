using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using RecipeManager.Models;
using SQLite;

namespace RecipeManager.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private bool _isInitialized;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "recipemanager_v1.db3");
    }

    private static void Log(string msg)
    {
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log"), $"[{System.DateTime.Now:HH:mm:ss.fff}] [DatabaseService] {msg}\r\n"); } catch {}
    }

    public async Task InitializeAsync()
    {
        Log("InitializeAsync entered");
        if (_isInitialized && _database != null)
        {
            Log("Already initialized");
            return;
        }

        try
        {
            SQLitePCL.Batteries_V2.Init();
            Log("SQLitePCL.Batteries_V2.Init() succeeded");

            var dir = Path.GetDirectoryName(_dbPath);
            Log($"DB Path: {_dbPath}, Dir: {dir}");
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Log("Directory created");
            }

            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
            Log("SQLiteAsyncConnection created");

            await _database.CreateTableAsync<CustomRecipe>();
            Log("CustomRecipe table created");
            await _database.CreateTableAsync<ShoppingItem>();
            Log("ShoppingItem table created");
            await _database.CreateTableAsync<MealPlanItem>();
            Log("MealPlanItem table created");
            await _database.CreateTableAsync<FavoriteRecipe>();
            Log("FavoriteRecipe table created");

            _isInitialized = true;

            // Seed sample data if empty
            await SeedInitialDataAsync();
            await SanitizeExistingDataAsync();
            Log("SeedInitialDataAsync and SanitizeExistingDataAsync completed");
        }
        catch (System.Exception ex)
        {
            Log($"InitializeAsync FAILED with exception: {ex}");
            throw;
        }
    }

    private async Task EnsureInitAsync()
    {
        if (!_isInitialized || _database == null)
        {
            await InitializeAsync();
        }
    }

    private async Task SanitizeExistingDataAsync()
    {
        try
        {
            if (_database == null) return;

            // Sanitize custom recipes
            var customRecipes = await _database.Table<CustomRecipe>().ToListAsync();
            foreach (var r in customRecipes)
            {
                if (r.IngredientsText.Contains("beef", StringComparison.OrdinalIgnoreCase) ||
                    r.Instructions.Contains("beef", StringComparison.OrdinalIgnoreCase) ||
                    r.Title.Contains("beef", StringComparison.OrdinalIgnoreCase))
                {
                    r.Title = r.Title.Replace("Beef", "Chicken", StringComparison.OrdinalIgnoreCase);
                    r.IngredientsText = r.IngredientsText.Replace("Ground Beef", "Minced Chicken", StringComparison.OrdinalIgnoreCase)
                                                        .Replace("beef", "chicken", StringComparison.OrdinalIgnoreCase);
                    r.Instructions = r.Instructions.Replace("ground beef", "minced chicken", StringComparison.OrdinalIgnoreCase)
                                                   .Replace("beef", "chicken", StringComparison.OrdinalIgnoreCase);
                    await _database.UpdateAsync(r);
                }
            }

            // Sanitize shopping items
            var shoppingItems = await _database.Table<ShoppingItem>().ToListAsync();
            foreach (var s in shoppingItems)
            {
                if (s.Name.Contains("beef", StringComparison.OrdinalIgnoreCase))
                {
                    s.Name = s.Name.Replace("beef", "chicken", StringComparison.OrdinalIgnoreCase)
                                   .Replace("Beef", "Chicken", StringComparison.OrdinalIgnoreCase);
                    await _database.UpdateAsync(s);
                }
            }

            // Remove beef favorites
            var favorites = await _database.Table<FavoriteRecipe>().ToListAsync();
            foreach (var f in favorites)
            {
                if (f.Title.Contains("beef", StringComparison.OrdinalIgnoreCase) ||
                    f.Category.Contains("beef", StringComparison.OrdinalIgnoreCase))
                {
                    await _database.DeleteAsync(f);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"SanitizeExistingDataAsync notice: {ex.Message}");
        }
    }

    private async Task SeedInitialDataAsync()
    {
        var existingRecipes = await _database!.Table<CustomRecipe>().CountAsync();
        if (existingRecipes == 0)
        {
            var seedRecipes = new List<CustomRecipe>
            {
                new CustomRecipe
                {
                    Title = "Royal Hyderabadi Dum Chicken Biryani",
                    Category = "Chicken",
                    PrepTimeMinutes = 30,
                    CookTimeMinutes = 45,
                    Servings = 6,
                    IngredientsText = "800g Fresh Chicken (bone-in pieces)\n500g Aged Long-Grain Basmati Rice\n200g Thick Plain Yogurt / Curd\n1.5 cups Golden Fried Onions (Birista)\n2 tbsp Fresh Ginger-Garlic Paste\n1 cup Fresh Mint Leaves, chopped\n1 cup Fresh Coriander Leaves, chopped\n4 Green Chilies, slit lengthwise\n1.5 tbsp Kashmiri Red Chili Powder\n1/2 tsp Turmeric Powder\n1.5 tbsp Royal Shahi Biryani Masala\nWhole Spices (Cardamom, Cloves, Cinnamon, Bay Leaves)\n4 tbsp Warm Saffron Milk\n4 tbsp Pure Desi Ghee\n1 tsp Kewra / Rose Water\n2 tbsp Fresh Lemon Juice\nSalt to taste",
                    Instructions = "1. Marination: In a deep bowl, coat chicken with yogurt, ginger-garlic paste, red chili powder, turmeric, biryani masala, half of the fried onions, mint, coriander, slit chilies, lemon juice, 2 tbsp oil, and salt. Massage thoroughly and refrigerate for 1-2 hours.\n2. Par-cooking the Rice: In a large stockpot, boil 3 liters of water with whole spices, 2 tbsp salt, and 1 tsp ghee. Add washed and soaked basmati rice. Cook for 5-6 minutes until 70% done. Drain immediately.\n3. Dum Assembly: In a heavy-bottomed pot, lay the marinated chicken in an even layer. Layer the steaming par-cooked basmati rice on top.\n4. Aromatics: Drizzle saffron milk, desi ghee, and kewra water. Scatter remaining crispy onions, mint, and fresh coriander.\n5. Dum Cooking: Seal pot rim with aluminum foil and press heavy lid. Cook on medium-high heat for 10 minutes, then place on a heavy tawa on low flame for 35 minutes.\n6. Rest & Serve: Rest 15 minutes before opening. Gently fluff from the edges. Serve hot with cucumber raita and spicy salan.",
                    ImageUrl = "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=800&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsFavorite = true
                },
                new CustomRecipe
                {
                    Title = "Nawabi Shahi Mutton Dum Biryani",
                    Category = "Mutton",
                    PrepTimeMinutes = 45,
                    CookTimeMinutes = 70,
                    Servings = 6,
                    IngredientsText = "800g Tender Goat Mutton / Lamb pieces\n500g Aged Royal Basmati Rice (soaked 40 mins)\n1 cup Plain Whisked Yogurt\n2 cups Crispy Golden Fried Onions (Birista)\n1 tbsp Raw Papaya Paste (meat tenderizer)\n2.5 tbsp Ginger-Garlic Paste\n2 tbsp Kashmiri Red Chili Powder\n2 tbsp Nawabi Biryani Garam Masala\n1.5 cups Fresh Mint and Coriander, chopped\nWhole Spices (Shahi Jeera, Black Cardamom, Star Anise, Cloves, Cinnamon)\n1/4 cup Warm Saffron Milk\n5 tbsp Desi Ghee\n5 Green Chilies, slit\n2 tbsp Lemon Juice\nSalt to taste",
                    Instructions = "1. Tenderization & Marination: Clean mutton pieces thoroughly. In a large bowl, mix mutton with raw papaya paste, ginger-garlic paste, yogurt, half the fried onions, chili powder, nawabi biryani masala, mint, coriander, green chilies, lemon juice, 2 tbsp ghee, and salt. Marinate in refrigerator for at least 4 hours (overnight recommended for melt-in-mouth tenderness).\n2. Cooking Basmati: Boil water with whole spices and salt. Add drained soaked basmati rice and cook until 70% done (grain breaks into three parts). Drain thoroughly.\n3. Dum Assembly: In a heavy cast iron or copper biryani pot, grease with 2 tbsp ghee and spread the marinated mutton pieces in a single layer at the base.\n4. Rice & Aromatic Layering: Spread the steaming drained basmati rice evenly over the mutton. Drizzle saffron milk, remaining desi ghee, and scatter crispy golden onions and freshly chopped mint.\n5. Slow Dum Infusion: Seal the pot securely with heavy-duty foil and a tight lid. Cook on high flame for 12 minutes to build deep steam pressure, then transfer to a low flame on a hot skillet for 55 minutes until mutton is juicy and tender.\n6. Rest & Serve: Rest for 15 minutes before unsealing. Gently mix with a flat spoon from bottom to top to preserve elongated rice grains. Serve with mirchi ka salan, spiced onion rings, and mint raita.",
                    ImageUrl = "https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=800&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.Now,
                    IsFavorite = true
                },
                new CustomRecipe
                {
                    Title = "Grandma's Rustic Herb Chicken Bolognese",
                    Category = "Pasta",
                    PrepTimeMinutes = 20,
                    CookTimeMinutes = 45,
                    Servings = 4,
                    IngredientsText = "400g Tagliatelle pasta\n500g Minced Chicken Breast\n1 Yellow onion, diced\n2 Garlic cloves, minced\n800g Crushed Tomatoes\n2 tbsp Olive Oil\nSalt and black pepper to taste\nFresh Parmesan cheese",
                    Instructions = "1. Heat olive oil in a heavy pot over medium heat.\n2. Sauté chopped onion and minced garlic until fragrant and translucent.\n3. Add minced chicken and brown thoroughly, breaking up any clumps.\n4. Pour in crushed tomatoes, season with salt and pepper, and simmer on low for 35 minutes.\n5. Cook tagliatelle in salted boiling water until al dente.\n6. Toss pasta with rich bolognese sauce and top with grated parmesan.",
                    ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3d5d6281691?w=500&auto=format&fit=crop&q=60",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    IsFavorite = true
                },
                new CustomRecipe
                {
                    Title = "Crispy Avocado Breakfast Toast",
                    Category = "Breakfast",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 5,
                    Servings = 2,
                    IngredientsText = "2 Slices sourdough bread\n1 Ripe Hass avocado\n2 Eggs (poached or fried)\n1 tsp Red chili flakes\n1 tbsp Extra virgin olive oil\nFlaky sea salt & cracked pepper",
                    Instructions = "1. Toast sourdough slices until golden and crispy.\n2. In a small bowl, mash ripe avocado with olive oil, salt, and pepper.\n3. Spread avocado generously over warm toast.\n4. Cook eggs sunny-side up or poached and place atop avocado.\n5. Garnish with red chili flakes and extra sea salt.",
                    ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=500&auto=format&fit=crop&q=60",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsFavorite = true
                }
            };

            await _database.InsertAllAsync(seedRecipes);
        }
        else
        {
            // Ensure Biryani recipes are present even if database was seeded in an earlier run
            var hasChickenBiryani = await _database.Table<CustomRecipe>().Where(r => r.Title.Contains("Chicken Biryani")).CountAsync() > 0;
            if (!hasChickenBiryani)
            {
                await _database.InsertAsync(new CustomRecipe
                {
                    Title = "Royal Hyderabadi Dum Chicken Biryani",
                    Category = "Chicken",
                    PrepTimeMinutes = 30,
                    CookTimeMinutes = 45,
                    Servings = 6,
                    IngredientsText = "800g Fresh Chicken (bone-in pieces)\n500g Aged Long-Grain Basmati Rice\n200g Thick Plain Yogurt / Curd\n1.5 cups Golden Fried Onions (Birista)\n2 tbsp Fresh Ginger-Garlic Paste\n1 cup Fresh Mint Leaves, chopped\n1 cup Fresh Coriander Leaves, chopped\n4 Green Chilies, slit lengthwise\n1.5 tbsp Kashmiri Red Chili Powder\n1/2 tsp Turmeric Powder\n1.5 tbsp Royal Shahi Biryani Masala\nWhole Spices (Cardamom, Cloves, Cinnamon, Bay Leaves)\n4 tbsp Warm Saffron Milk\n4 tbsp Pure Desi Ghee\n1 tsp Kewra / Rose Water\n2 tbsp Fresh Lemon Juice\nSalt to taste",
                    Instructions = "1. Marination: In a deep bowl, coat chicken with yogurt, ginger-garlic paste, red chili powder, turmeric, biryani masala, half of the fried onions, mint, coriander, slit chilies, lemon juice, 2 tbsp oil, and salt. Massage thoroughly and refrigerate for 1-2 hours.\n2. Par-cooking the Rice: In a large stockpot, boil 3 liters of water with whole spices, 2 tbsp salt, and 1 tsp ghee. Add washed and soaked basmati rice. Cook for 5-6 minutes until 70% done. Drain immediately.\n3. Dum Assembly: In a heavy-bottomed pot, lay the marinated chicken in an even layer. Layer the steaming par-cooked basmati rice on top.\n4. Aromatics: Drizzle saffron milk, desi ghee, and kewra water. Scatter remaining crispy onions, mint, and fresh coriander.\n5. Dum Cooking: Seal pot rim with aluminum foil and press heavy lid. Cook on medium-high heat for 10 minutes, then place on a heavy tawa on low flame for 35 minutes.\n6. Rest & Serve: Rest 15 minutes before opening. Gently fluff from the edges. Serve hot with cucumber raita and spicy salan.",
                    ImageUrl = "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=800&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsFavorite = true
                });
            }

            var hasMuttonBiryani = await _database.Table<CustomRecipe>().Where(r => r.Title.Contains("Mutton Biryani")).CountAsync() > 0;
            if (!hasMuttonBiryani)
            {
                await _database.InsertAsync(new CustomRecipe
                {
                    Title = "Nawabi Shahi Mutton Dum Biryani",
                    Category = "Mutton",
                    PrepTimeMinutes = 45,
                    CookTimeMinutes = 70,
                    Servings = 6,
                    IngredientsText = "800g Tender Goat Mutton / Lamb pieces\n500g Aged Royal Basmati Rice (soaked 40 mins)\n1 cup Plain Whisked Yogurt\n2 cups Crispy Golden Fried Onions (Birista)\n1 tbsp Raw Papaya Paste (meat tenderizer)\n2.5 tbsp Ginger-Garlic Paste\n2 tbsp Kashmiri Red Chili Powder\n2 tbsp Nawabi Biryani Garam Masala\n1.5 cups Fresh Mint and Coriander, chopped\nWhole Spices (Shahi Jeera, Black Cardamom, Star Anise, Cloves, Cinnamon)\n1/4 cup Warm Saffron Milk\n5 tbsp Desi Ghee\n5 Green Chilies, slit\n2 tbsp Lemon Juice\nSalt to taste",
                    Instructions = "1. Tenderization & Marination: Clean mutton pieces thoroughly. In a large bowl, mix mutton with raw papaya paste, ginger-garlic paste, yogurt, half the fried onions, chili powder, nawabi biryani masala, mint, coriander, green chilies, lemon juice, 2 tbsp ghee, and salt. Marinate in refrigerator for at least 4 hours (overnight recommended for melt-in-mouth tenderness).\n2. Cooking Basmati: Boil water with whole spices and salt. Add drained soaked basmati rice and cook until 70% done (grain breaks into three parts). Drain thoroughly.\n3. Dum Assembly: In a heavy cast iron or copper biryani pot, grease with 2 tbsp ghee and spread the marinated mutton pieces in a single layer at the base.\n4. Rice & Aromatic Layering: Spread the steaming drained basmati rice evenly over the mutton. Drizzle saffron milk, remaining desi ghee, and scatter crispy golden onions and freshly chopped mint.\n5. Slow Dum Infusion: Seal the pot securely with heavy-duty foil and a tight lid. Cook on high flame for 12 minutes to build deep steam pressure, then transfer to a low flame on a hot skillet for 55 minutes until mutton is juicy and tender.\n6. Rest & Serve: Rest for 15 minutes before unsealing. Gently mix with a flat spoon from bottom to top to preserve elongated rice grains. Serve with mirchi ka salan, spiced onion rings, and mint raita.",
                    ImageUrl = "https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=800&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.Now,
                    IsFavorite = true
                });
            }
        }

        var existingShopping = await _database.Table<ShoppingItem>().CountAsync();
        if (existingShopping == 0)
        {
            var seedShopping = new List<ShoppingItem>
            {
                new() { Name = "Extra Virgin Olive Oil", Measure = "1 bottle", Category = "Pantry", IsPurchased = false, RecipeSource = "Pantry Essentials" },
                new() { Name = "Fresh Basil Leaves", Measure = "1 bunch", Category = "Produce", IsPurchased = false, RecipeSource = "Pasta Bolognese" },
                new() { Name = "Parmigiano Reggiano", Measure = "200g", Category = "Dairy", IsPurchased = true, RecipeSource = "Pasta Bolognese" },
                new() { Name = "Sourdough Bread", Measure = "1 loaf", Category = "Bakery", IsPurchased = false, RecipeSource = "Avocado Toast" }
            };

            await _database.InsertAllAsync(seedShopping);
        }

        var existingPlans = await _database.Table<MealPlanItem>().CountAsync();
        if (existingPlans == 0)
        {
            var today = DateTime.Today;
            var seedPlans = new List<MealPlanItem>
            {
                new() { PlanDate = today, MealType = "Breakfast", RecipeTitle = "Crispy Avocado Breakfast Toast", RecipeImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=500&auto=format&fit=crop&q=60", Notes = "With poached egg" },
                new() { PlanDate = today, MealType = "Dinner", RecipeTitle = "Grandma's Rustic Pasta Bolognese", RecipeImageUrl = "https://images.unsplash.com/photo-1621996346565-e3d5d6281691?w=500&auto=format&fit=crop&q=60", Notes = "Family dinner" },
                new() { PlanDate = today.AddDays(1), MealType = "Lunch", RecipeTitle = "Teriyaki Chicken Casserole", RecipeImageUrl = "https://www.themealdb.com/images/media/meals/wvpsxx1468256321.jpg", Notes = "Meal prep lunch" },
                new() { PlanDate = today.AddDays(2), MealType = "Dinner", RecipeTitle = "Cajun Spiced Salmon", RecipeImageUrl = "https://www.themealdb.com/images/media/meals/450zjf1587342790.jpg", Notes = "Serve with asparagus" }
            };

            await _database.InsertAllAsync(seedPlans);
        }
    }

    // --- Custom Recipes ---
    public async Task<List<CustomRecipe>> GetCustomRecipesAsync()
    {
        await EnsureInitAsync();
        return await _database!.Table<CustomRecipe>().OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<CustomRecipe?> GetCustomRecipeByIdAsync(int id)
    {
        await EnsureInitAsync();
        return await _database!.Table<CustomRecipe>().Where(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveCustomRecipeAsync(CustomRecipe recipe)
    {
        await EnsureInitAsync();
        if (recipe.Id != 0)
        {
            return await _database!.UpdateAsync(recipe);
        }
        recipe.CreatedAt = DateTime.Now;
        return await _database!.InsertAsync(recipe);
    }

    public async Task<int> DeleteCustomRecipeAsync(CustomRecipe recipe)
    {
        await EnsureInitAsync();
        return await _database!.DeleteAsync(recipe);
    }

    // --- Shopping List ---
    public async Task<List<ShoppingItem>> GetShoppingItemsAsync()
    {
        await EnsureInitAsync();
        return await _database!.Table<ShoppingItem>().OrderBy(i => i.IsPurchased).ThenByDescending(i => i.Id).ToListAsync();
    }

    public async Task<int> SaveShoppingItemAsync(ShoppingItem item)
    {
        await EnsureInitAsync();
        if (item.Id != 0)
        {
            return await _database!.UpdateAsync(item);
        }
        item.CreatedAt = DateTime.Now;
        return await _database!.InsertAsync(item);
    }

    public async Task<int> DeleteShoppingItemAsync(ShoppingItem item)
    {
        await EnsureInitAsync();
        return await _database!.DeleteAsync(item);
    }

    public async Task ClearPurchasedShoppingItemsAsync()
    {
        await EnsureInitAsync();
        await _database!.ExecuteAsync("DELETE FROM shopping_items WHERE IsPurchased = 1");
    }

    public async Task AddIngredientsToShoppingListAsync(IEnumerable<RecipeIngredient> ingredients, string recipeSource)
    {
        await EnsureInitAsync();
        var itemsToAdd = new List<ShoppingItem>();

        foreach (var ing in ingredients)
        {
            if (string.IsNullOrWhiteSpace(ing.Name))
                continue;

            itemsToAdd.Add(new ShoppingItem
            {
                Name = ing.Name.Trim(),
                Measure = ing.Measure?.Trim() ?? string.Empty,
                Category = DetermineIngredientCategory(ing.Name),
                IsPurchased = false,
                RecipeSource = recipeSource,
                CreatedAt = DateTime.Now
            });
        }

        if (itemsToAdd.Count > 0)
        {
            await _database!.InsertAllAsync(itemsToAdd);
        }
    }

    private string DetermineIngredientCategory(string ingredientName)
    {
        var lower = ingredientName.ToLower();
        if (lower.Contains("chicken") || lower.Contains("turkey") || lower.Contains("salmon") || lower.Contains("shrimp") || lower.Contains("fish") || lower.Contains("tuna") || lower.Contains("pork"))
            return "Poultry & Seafood";
        if (lower.Contains("milk") || lower.Contains("cheese") || lower.Contains("butter") || lower.Contains("cream") || lower.Contains("yogurt") || lower.Contains("parmesan"))
            return "Dairy";
        if (lower.Contains("onion") || lower.Contains("garlic") || lower.Contains("tomato") || lower.Contains("lemon") || lower.Contains("basil") || lower.Contains("parsley") || lower.Contains("avocado") || lower.Contains("carrot") || lower.Contains("broccoli") || lower.Contains("pepper") || lower.Contains("potato"))
            return "Produce";
        if (lower.Contains("flour") || lower.Contains("sugar") || lower.Contains("rice") || lower.Contains("pasta") || lower.Contains("oil") || lower.Contains("salt") || lower.Contains("cumin") || lower.Contains("powder") || lower.Contains("sauce"))
            return "Pantry";
        if (lower.Contains("bread") || lower.Contains("tortilla") || lower.Contains("bun") || lower.Contains("bagel"))
            return "Bakery";

        return "General";
    }

    // --- Meal Plans ---
    public async Task<List<MealPlanItem>> GetMealPlansAsync(DateTime? date = null)
    {
        await EnsureInitAsync();
        if (date.HasValue)
        {
            var dayStart = date.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            return await _database!.Table<MealPlanItem>()
                .Where(m => m.PlanDate >= dayStart && m.PlanDate < dayEnd)
                .OrderBy(m => m.MealType)
                .ToListAsync();
        }

        return await _database!.Table<MealPlanItem>().OrderBy(m => m.PlanDate).ToListAsync();
    }

    public async Task<List<MealPlanItem>> GetWeekMealPlansAsync(DateTime startDate)
    {
        await EnsureInitAsync();
        var start = startDate.Date;
        var end = start.AddDays(7);
        return await _database!.Table<MealPlanItem>()
            .Where(m => m.PlanDate >= start && m.PlanDate < end)
            .OrderBy(m => m.PlanDate)
            .ThenBy(m => m.MealType)
            .ToListAsync();
    }

    public async Task<int> SaveMealPlanAsync(MealPlanItem plan)
    {
        await EnsureInitAsync();
        if (plan.Id != 0)
        {
            return await _database!.UpdateAsync(plan);
        }
        return await _database!.InsertAsync(plan);
    }

    public async Task<int> DeleteMealPlanAsync(MealPlanItem plan)
    {
        await EnsureInitAsync();
        return await _database!.DeleteAsync(plan);
    }

    // --- Favorites ---
    public async Task<List<FavoriteRecipe>> GetFavoritesAsync()
    {
        await EnsureInitAsync();
        return await _database!.Table<FavoriteRecipe>().OrderByDescending(f => f.SavedAt).ToListAsync();
    }

    public async Task<bool> IsFavoriteAsync(string recipeId)
    {
        await EnsureInitAsync();
        var item = await _database!.Table<FavoriteRecipe>().Where(f => f.RecipeId == recipeId).FirstOrDefaultAsync();
        return item != null;
    }

    public async Task<bool> ToggleFavoriteAsync(Recipe recipe)
    {
        await EnsureInitAsync();
        var existing = await _database!.Table<FavoriteRecipe>().Where(f => f.RecipeId == recipe.Id).FirstOrDefaultAsync();
        if (existing != null)
        {
            await _database.DeleteAsync(existing);
            return false;
        }

        var fav = new FavoriteRecipe
        {
            RecipeId = recipe.Id,
            Title = recipe.Title,
            Category = recipe.Category,
            Area = recipe.Area,
            ImageUrl = recipe.ImageUrl,
            SavedAt = DateTime.Now
        };
        await _database.InsertAsync(fav);
        return true;
    }
}
