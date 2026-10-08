using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using RecipeManager.ViewModels;

namespace RecipeManager.Views;

public partial class RecipeDetailPage : ContentPage
{
    public RecipeDetailPage() : this(IPlatformApplication.Current!.Services.GetRequiredService<RecipeDetailViewModel>())
    {
    }

    public RecipeDetailPage(RecipeDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
