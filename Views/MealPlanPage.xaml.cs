using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using RecipeManager.ViewModels;

namespace RecipeManager.Views;

public partial class MealPlanPage : ContentPage
{
    private readonly MealPlanViewModel _viewModel;

    public MealPlanPage() : this(IPlatformApplication.Current!.Services.GetRequiredService<MealPlanViewModel>())
    {
    }

    public MealPlanPage(MealPlanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMealsForDateAsync();
    }
}
