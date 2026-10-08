using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using RecipeManager.ViewModels;

namespace RecipeManager.Views;

public partial class RecipesPage : ContentPage
{
    private readonly RecipesViewModel _viewModel;

    private static void Log(string msg)
    {
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log"), $"[{System.DateTime.Now:HH:mm:ss.fff}] [RecipesPage] {msg}\r\n"); } catch {}
    }

    public RecipesPage() : this(IPlatformApplication.Current!.Services.GetRequiredService<RecipesViewModel>())
    {
        Log("RecipesPage() parameterless ctor");
    }

    public RecipesPage(RecipesViewModel viewModel)
    {
        Log("RecipesPage() parameterized ctor entry");
        try
        {
            InitializeComponent();
            Log("RecipesPage InitializeComponent() succeeded");
        }
        catch (System.Exception ex)
        {
            Log($"RecipesPage InitializeComponent() failed: {ex}");
            throw;
        }
        BindingContext = _viewModel = viewModel;
        Log("RecipesPage ctor completed");
    }

    protected override async void OnAppearing()
    {
        Log("RecipesPage OnAppearing() entry");
        base.OnAppearing();
        try
        {
            await _viewModel.InitializeAsync();
            Log("RecipesPage OnAppearing() completed");
        }
        catch (System.Exception ex)
        {
            Log($"RecipesPage OnAppearing() exception: {ex}");
        }
    }
}
