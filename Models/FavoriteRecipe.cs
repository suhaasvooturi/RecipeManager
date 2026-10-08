using SQLite;
using System;

namespace RecipeManager.Models;

[Table("favorite_recipes")]
public class FavoriteRecipe
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string RecipeId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime SavedAt { get; set; } = DateTime.Now;
}
