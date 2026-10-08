using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using RecipeManager.Models;
using RecipeManager.Services;

namespace RecipeManager.ViewModels;

[QueryProperty(nameof(Recipe), "Recipe")]
public partial class RecipeDetailViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private Recipe? _recipe;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private int _servings = 4;

    [ObservableProperty]
    private string _servingsText = "4 Servings";

    public ObservableCollection<RecipeIngredient> Ingredients { get; } = new();
    public ObservableCollection<InstructionStepItem> Steps { get; } = new();

    public RecipeDetailViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Recipe Details";
    }

    partial void OnRecipeChanged(Recipe? value)
    {
        if (value == null) return;

        Title = value.Title;
        Servings = 4;
        ServingsText = "4 Servings";

        Ingredients.Clear();
        foreach (var ing in value.Ingredients)
        {
            ing.ScaledMeasure = ing.Measure;
            Ingredients.Add(ing);
        }

        Steps.Clear();
        if (!string.IsNullOrWhiteSpace(value.Instructions))
        {
            var rawSteps = value.Instructions.Split(new[] { "\r\n\r\n", "\n\n", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int stepNum = 1;
            foreach (var step in rawSteps)
            {
                var clean = step.Trim();
                if (clean.Length > 5)
                {
                    // Strip leading numbers if already present like "1. " or "STEP 1:"
                    if (char.IsDigit(clean[0]) && clean.Length > 2 && (clean[1] == '.' || clean[1] == ')'))
                    {
                        clean = clean.Substring(2).Trim();
                    }
                    else if (clean.StartsWith("STEP", StringComparison.OrdinalIgnoreCase))
                    {
                        var idx = clean.IndexOf(':');
                        if (idx > 0 && idx < clean.Length - 1)
                            clean = clean.Substring(idx + 1).Trim();
                    }

                    Steps.Add(new InstructionStepItem
                    {
                        StepNumber = stepNum,
                        Instruction = clean,
                        IsCompleted = false
                    });
                    stepNum++;
                }
            }
        }

        CheckFavoriteStatus();
    }

    private async void CheckFavoriteStatus()
    {
        if (Recipe == null) return;
        IsFavorite = await _databaseService.IsFavoriteAsync(Recipe.Id);
    }

    [RelayCommand]
    public void IncreaseServings()
    {
        if (Servings >= 12) return;
        Servings += 2;
        UpdateScaledServings();
    }

    [RelayCommand]
    public void DecreaseServings()
    {
        if (Servings <= 2) return;
        Servings -= 2;
        UpdateScaledServings();
    }

    private void UpdateScaledServings()
    {
        ServingsText = $"{Servings} Servings";
        double factor = Servings / 4.0;

        foreach (var ing in Ingredients)
        {
            ing.ScaledMeasure = ScaleMeasure(ing.Measure, factor);
        }
    }

    private static string ScaleMeasure(string measure, double factor)
    {
        if (string.IsNullOrWhiteSpace(measure) || Math.Abs(factor - 1.0) < 0.01)
            return measure;

        // Try regex match leading number or fraction (e.g., "400g", "2 tbsp", "1/2 cup")
        var match = Regex.Match(measure.Trim(), @"^(\d+(\.\d+)?|\d+/\d+)");
        if (match.Success)
        {
            var numStr = match.Value;
            double numVal;
            if (numStr.Contains('/'))
            {
                var parts = numStr.Split('/');
                if (double.TryParse(parts[0], out var num) && double.TryParse(parts[1], out var den) && den > 0)
                    numVal = num / den;
                else
                    return $"{measure} (x{factor:0.#})";
            }
            else if (!double.TryParse(numStr, out numVal))
            {
                return $"{measure} (x{factor:0.#})";
            }

            double scaled = numVal * factor;
            string formattedNum = scaled % 1 == 0 ? $"{scaled:0}" : $"{scaled:0.##}";
            return measure.Trim().Substring(match.Length).Trim() is var rest && string.IsNullOrEmpty(rest)
                ? formattedNum
                : $"{formattedNum} {rest}".Trim();
        }

        return $"{measure} (x{factor:0.#})";
    }

    [RelayCommand]
    public void ToggleStep(InstructionStepItem? step)
    {
        if (step == null) return;
        step.IsCompleted = !step.IsCompleted;
    }

    [RelayCommand]
    public async Task AddToShoppingListAsync()
    {
        if (Recipe == null || Recipe.Ingredients.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Shopping List", "No ingredient list found for this recipe.", "OK");
            return;
        }

        await _databaseService.AddIngredientsToShoppingListAsync(Recipe.Ingredients, Recipe.Title);
        await Shell.Current.DisplayAlertAsync("Added to Shopping List", $"{Recipe.Ingredients.Count} ingredients added from '{Recipe.Title}' to your Shopping List!", "OK");
    }

    [RelayCommand]
    public async Task AddToMealPlanAsync()
    {
        if (Recipe == null) return;

        string action = await Shell.Current.DisplayActionSheetAsync(
            $"Schedule '{Recipe.Title}' for:", "Cancel", null,
            "Today - Breakfast", "Today - Lunch", "Today - Dinner",
            "Tomorrow - Breakfast", "Tomorrow - Lunch", "Tomorrow - Dinner");

        if (string.IsNullOrEmpty(action) || action == "Cancel")
            return;

        var date = action.StartsWith("Tomorrow") ? DateTime.Today.AddDays(1) : DateTime.Today;
        var mealType = action.Split('-')[1].Trim();

        var planItem = new MealPlanItem
        {
            PlanDate = date,
            MealType = mealType,
            RecipeId = Recipe.Id,
            RecipeTitle = Recipe.Title,
            RecipeImageUrl = Recipe.ImageUrl,
            Notes = $"From {Recipe.Category}"
        };

        await _databaseService.SaveMealPlanAsync(planItem);
        await Shell.Current.DisplayAlertAsync("Meal Planned", $"'{Recipe.Title}' scheduled for {date:MMM dd} ({mealType})!", "OK");
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync()
    {
        if (Recipe == null) return;

        IsFavorite = await _databaseService.ToggleFavoriteAsync(Recipe);
        var msg = IsFavorite ? "Recipe saved to your favorites!" : "Recipe removed from favorites.";
        await Shell.Current.DisplayAlertAsync("Favorites", msg, "OK");
    }

    [RelayCommand]
    public async Task ShareRecipeAsync()
    {
        if (Recipe == null) return;

        var sb = new StringBuilder();
        sb.AppendLine($"🍽️ {Recipe.Title}");
        sb.AppendLine($"Category: {Recipe.Category} | Cuisine: {Recipe.CuisineFlag}");
        sb.AppendLine($"Servings: {Servings} | Rating: {Recipe.RatingText}");
        sb.AppendLine();
        sb.AppendLine("📋 Ingredients:");
        foreach (var ing in Ingredients)
        {
            sb.AppendLine($"- {ing.DisplayText}");
        }
        sb.AppendLine();
        sb.AppendLine("👨‍🍳 Instructions:");
        foreach (var s in Steps)
        {
            sb.AppendLine($"Step {s.StepNumber}: {s.Instruction}");
        }

        await Clipboard.Default.SetTextAsync(sb.ToString());
        await Shell.Current.DisplayAlertAsync("Recipe Copied", "Recipe ingredients and instructions copied to clipboard! Ready to share.", "OK");
    }

    [RelayCommand]
    public async Task OpenYoutubeTutorialAsync()
    {
        if (string.IsNullOrWhiteSpace(Recipe?.YoutubeUrl))
        {
            await Shell.Current.DisplayAlertAsync("Video Tutorial", "No video link available for this recipe.", "OK");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new Uri(Recipe.YoutubeUrl));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not open video: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
