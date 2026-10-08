using SQLite;
using System;

namespace RecipeManager.Models;

[Table("custom_recipes")]
public class CustomRecipe
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(150), NotNull]
    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = "Main Dish";

    public int PrepTimeMinutes { get; set; } = 15;

    public int CookTimeMinutes { get; set; } = 30;

    public int Servings { get; set; } = 4;

    public string IngredientsText { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = "https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&auto=format&fit=crop&q=60";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsFavorite { get; set; }

    [Ignore]
    public string TotalTimeString => $"{PrepTimeMinutes + CookTimeMinutes} mins";

    [Ignore]
    public string ServingsString => $"{Servings} servings";
}
