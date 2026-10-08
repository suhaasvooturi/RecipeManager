using SQLite;
using System;

namespace RecipeManager.Models;

[Table("meal_plans")]
public class MealPlanItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime PlanDate { get; set; } = DateTime.Today;

    // Breakfast, Lunch, Dinner, Snack
    [MaxLength(50), NotNull]
    public string MealType { get; set; } = "Dinner";

    public string RecipeId { get; set; } = string.Empty;

    [MaxLength(150), NotNull]
    public string RecipeTitle { get; set; } = string.Empty;

    public string RecipeImageUrl { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    [Ignore]
    public string FormattedDate => PlanDate.ToString("ddd, MMM dd");

    [Ignore]
    public string DayName => PlanDate.ToString("dddd");
}
