using SQLite;
using System;

namespace RecipeManager.Models;

[Table("shopping_items")]
public class ShoppingItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string Measure { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public bool IsPurchased { get; set; }

    public string RecipeSource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string DisplayText => string.IsNullOrWhiteSpace(Measure) 
        ? Name 
        : $"{Measure} {Name}";
}
