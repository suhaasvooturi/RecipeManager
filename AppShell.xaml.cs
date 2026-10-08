using Microsoft.Maui.Controls;
using RecipeManager.Views;

namespace RecipeManager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("RecipeDetailPage", typeof(RecipeDetailPage));
        Routing.RegisterRoute("AddRecipePage", typeof(AddRecipePage));
    }
}
