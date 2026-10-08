using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecipeManager.Services;
using RecipeManager.ViewModels;
using RecipeManager.Views;

namespace RecipeManager;

public static class MauiProgram
{
    private static void Log(string msg)
    {
        try
        {
            var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log");
            System.IO.File.AppendAllText(path, $"[{System.DateTime.Now:HH:mm:ss.fff}] [MauiProgram] {msg}\r\n");
        }
        catch {}
    }

    public static MauiApp CreateMauiApp()
    {
        Log("CreateMauiApp entry");
        var builder = MauiApp.CreateBuilder();
        Log("CreateBuilder succeeded");

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        Log("UseMauiApp and ConfigureFonts configured");

        // Register Services
        builder.Services.AddSingleton<IRecipeApiService, RecipeApiService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        Log("Services registered");

        // Register ViewModels
        builder.Services.AddSingleton<RecipesViewModel>();
        builder.Services.AddTransient<RecipeDetailViewModel>();
        builder.Services.AddTransient<AddRecipeViewModel>();
        builder.Services.AddSingleton<ShoppingListViewModel>();
        builder.Services.AddSingleton<MealPlanViewModel>();
        builder.Services.AddSingleton<FavoritesViewModel>();
        Log("ViewModels registered");

        // Register Views
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddSingleton<RecipesPage>();
        builder.Services.AddTransient<RecipeDetailPage>();
        builder.Services.AddTransient<AddRecipePage>();
        builder.Services.AddSingleton<ShoppingListPage>();
        builder.Services.AddSingleton<MealPlanPage>();
        builder.Services.AddSingleton<FavoritesPage>();
        Log("Views registered");

#if DEBUG
        builder.Logging.AddDebug();
#endif

        Log("Calling builder.Build()...");
        var app = builder.Build();
        Log("builder.Build() completed!");
        return app;
    }
}
