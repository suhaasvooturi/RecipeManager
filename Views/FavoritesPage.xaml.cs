using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using RecipeManager.ViewModels;

namespace RecipeManager.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _viewModel;

    public FavoritesPage() : this(IPlatformApplication.Current!.Services.GetRequiredService<FavoritesViewModel>())
    {
    }

    public FavoritesPage(FavoritesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}
