using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using RecipeManager.Models;
using RecipeManager.Services;

namespace RecipeManager.ViewModels;

public partial class DayItem : ObservableObject
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class MealPlanViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IRecipeApiService _apiService;

    public ObservableCollection<MealPlanItem> MealsForDay { get; } = new();
    public ObservableCollection<DayItem> WeekDays { get; } = new();

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _customMealTitle = string.Empty;

    [ObservableProperty]
    private string _selectedMealType = "Dinner";

    public ObservableCollection<string> MealTypes { get; } = new()
    {
        "Breakfast", "Lunch", "Dinner", "Snack"
    };

    [ObservableProperty]
    private string _formattedDayHeader = string.Empty;

    public MealPlanViewModel(IDatabaseService databaseService, IRecipeApiService apiService)
    {
        _databaseService = databaseService;
        _apiService = apiService;
        Title = "Meal Planner";
        UpdateDayHeader();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateDayHeader();
        _ = LoadMealsForDateAsync();
    }

    private void UpdateDayHeader()
    {
        if (SelectedDate.Date == DateTime.Today)
            FormattedDayHeader = $"Today, {SelectedDate:MMM dd}";
        else if (SelectedDate.Date == DateTime.Today.AddDays(1))
            FormattedDayHeader = $"Tomorrow, {SelectedDate:MMM dd}";
        else
            FormattedDayHeader = $"{SelectedDate:dddd, MMM dd}";

        UpdateWeekDays();
    }

    private void UpdateWeekDays()
    {
        WeekDays.Clear();
        // Calculate Monday of the selected week
        int diff = (7 + (SelectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = SelectedDate.Date.AddDays(-diff);
        for (int i = 0; i < 7; i++)
        {
            var d = monday.AddDays(i);
            WeekDays.Add(new DayItem
            {
                Date = d,
                DayName = d.ToString("ddd"),
                DayNumber = d.ToString("dd"),
                IsSelected = (d.Date == SelectedDate.Date)
            });
        }
    }

    [RelayCommand]
    public void SelectDay(DayItem? day)
    {
        if (day == null) return;
        SelectedDate = day.Date;
    }

    [RelayCommand]
    public async Task LoadMealsForDateAsync()
    {
        IsBusy = true;
        try
        {
            var plans = await _databaseService.GetMealPlansAsync(SelectedDate);
            MealsForDay.Clear();
            foreach (var item in plans)
            {
                MealsForDay.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Load error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void GoToPreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    public void GoToNextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    [RelayCommand]
    public void GoToToday()
    {
        SelectedDate = DateTime.Today;
    }

    [RelayCommand]
    public async Task AddCustomMealAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomMealTitle))
        {
            await Shell.Current.DisplayAlertAsync("Meal Planner", "Please enter a recipe or dish name.", "OK");
            return;
        }

        var plan = new MealPlanItem
        {
            PlanDate = SelectedDate.Date,
            MealType = SelectedMealType,
            RecipeTitle = CustomMealTitle.Trim(),
            RecipeImageUrl = "https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=500&auto=format&fit=crop&q=60",
            Notes = "Custom scheduled meal"
        };

        await _databaseService.SaveMealPlanAsync(plan);
        MealsForDay.Add(plan);

        CustomMealTitle = string.Empty;
    }

    [RelayCommand]
    public async Task DeleteMealPlanAsync(MealPlanItem? item)
    {
        if (item == null) return;

        await _databaseService.DeleteMealPlanAsync(item);
        MealsForDay.Remove(item);
    }

    [RelayCommand]
    public async Task AutoRecommendMealAsync()
    {
        IsBusy = true;
        try
        {
            var recipe = await _apiService.GetRandomRecipeAsync();
            if (recipe == null) return;

            var plan = new MealPlanItem
            {
                PlanDate = SelectedDate.Date,
                MealType = SelectedMealType,
                RecipeId = recipe.Id,
                RecipeTitle = recipe.Title,
                RecipeImageUrl = recipe.ImageUrl,
                Notes = $"Chef's Spotlight ({recipe.Category})"
            };

            await _databaseService.SaveMealPlanAsync(plan);
            MealsForDay.Add(plan);

            await Shell.Current.DisplayAlertAsync("Chef's Pick Scheduled", $"Added '{recipe.Title}' for {SelectedMealType}!", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Notice", $"Could not load suggestion: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task GenerateWeeklyShoppingListAsync()
    {
        IsBusy = true;
        try
        {
            var startOfWeek = SelectedDate.Date.AddDays(-(int)SelectedDate.DayOfWeek);
            var weekPlans = await _databaseService.GetWeekMealPlansAsync(startOfWeek);

            if (weekPlans.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Meal Planner", "No meals scheduled for this week.", "OK");
                return;
            }

            int addedCount = 0;
            foreach (var plan in weekPlans)
            {
                if (!string.IsNullOrEmpty(plan.RecipeId))
                {
                    var recipe = await _apiService.GetRecipeByIdAsync(plan.RecipeId);
                    if (recipe?.Ingredients != null && recipe.Ingredients.Count > 0)
                    {
                        await _databaseService.AddIngredientsToShoppingListAsync(recipe.Ingredients, $"{plan.RecipeTitle} ({plan.MealType})");
                        addedCount += recipe.Ingredients.Count;
                    }
                }
                else
                {
                    // For custom meal without explicit ingredients list, add the dish itself as an item
                    var shoppingItem = new ShoppingItem
                    {
                        Name = plan.RecipeTitle,
                        Measure = "1 batch",
                        Category = "Meal Prep",
                        RecipeSource = $"{plan.MealType} on {plan.PlanDate:MMM dd}",
                        CreatedAt = DateTime.Now
                    };
                    await _databaseService.SaveShoppingItemAsync(shoppingItem);
                    addedCount++;
                }
            }

            await Shell.Current.DisplayAlertAsync("Shopping List Updated", 
                $"Generated ingredients for {weekPlans.Count} scheduled meals. {addedCount} item(s) added to Shopping List!", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not generate list: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
