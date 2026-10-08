using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using RecipeManager.ViewModels;

namespace RecipeManager.Views;

public partial class AddRecipePage : ContentPage
{
    public AddRecipePage() : this(IPlatformApplication.Current!.Services.GetRequiredService<AddRecipeViewModel>())
    {
    }

    public AddRecipePage(AddRecipeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
