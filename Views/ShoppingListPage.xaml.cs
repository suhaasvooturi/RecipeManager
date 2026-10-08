using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using RecipeManager.ViewModels;

namespace RecipeManager.Views;

public partial class ShoppingListPage : ContentPage
{
    private readonly ShoppingListViewModel _viewModel;

    public ShoppingListPage() : this(IPlatformApplication.Current!.Services.GetRequiredService<ShoppingListViewModel>())
    {
    }

    public ShoppingListPage(ShoppingListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadItemsAsync();
    }
}
