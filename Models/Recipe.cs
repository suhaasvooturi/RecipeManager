using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RecipeManager.Models;

public partial class RecipeIngredient : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Measure { get; set; } = string.Empty;

    [ObservableProperty]
    private string _scaledMeasure = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(ScaledMeasure)
        ? (string.IsNullOrWhiteSpace(Measure) ? Name : $"{Measure.Trim()} {Name.Trim()}")
        : $"{ScaledMeasure.Trim()} {Name.Trim()}";
}

public partial class Recipe : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? YoutubeUrl { get; set; }
    public string? SourceUrl { get; set; }
    public string? Tags { get; set; }
    public bool IsCustom { get; set; }

    [ObservableProperty]
    private bool _isFavorite;

    public List<RecipeIngredient> Ingredients { get; set; } = new();

    public string IngredientsSummary => Ingredients.Count > 0 
        ? $"{Ingredients.Count} ingredients" 
        : "Ingredients listed in instructions";

    public string CategoryAreaSubtitle => !string.IsNullOrEmpty(Area) 
        ? $"{Category} • {CuisineFlag}" 
        : Category;

    public string EstimatedTimeText
    {
        get
        {
            if (Category.Equals("Dessert", StringComparison.OrdinalIgnoreCase)) return "⏱️ 20 mins";
            if (Category.Equals("Breakfast", StringComparison.OrdinalIgnoreCase)) return "⏱️ 15 mins";
            if (Category.Equals("Pasta", StringComparison.OrdinalIgnoreCase)) return "⏱️ 30 mins";
            if (Category.Equals("Seafood", StringComparison.OrdinalIgnoreCase)) return "⏱️ 25 mins";
            if (Category.Equals("Chicken", StringComparison.OrdinalIgnoreCase)) return "⏱️ 35 mins";
            return "⏱️ 25 mins";
        }
    }

    public string CuisineFlag
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Area)) return "🌍 Global";
            var a = Area.ToLower();
            if (a.Contains("italian")) return "🇮🇹 Italian";
            if (a.Contains("japanese")) return "🇯🇵 Japanese";
            if (a.Contains("mexican")) return "🇲🇽 Mexican";
            if (a.Contains("american")) return "🇺🇸 American";
            if (a.Contains("french")) return "🇫🇷 French";
            if (a.Contains("chinese")) return "🇨🇳 Chinese";
            if (a.Contains("indian")) return "🇮🇳 Indian";
            if (a.Contains("greek")) return "🇬🇷 Greek";
            if (a.Contains("turkish")) return "🇹🇷 Turkish";
            if (a.Contains("british")) return "🇬🇧 British";
            if (a.Contains("spanish")) return "🇪🇸 Spanish";
            return $"🌍 {Area}";
        }
    }

    public string RatingText => "★ 4.9";
}
