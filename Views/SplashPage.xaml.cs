using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace RecipeManager.Views;

public partial class SplashPage : ContentPage
{
    private bool _hasStartedAnimation;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_hasStartedAnimation)
        {
            _hasStartedAnimation = true;
            _ = RunSplashAnimationAsync();
        }
    }

    private async Task RunSplashAnimationAsync()
    {
        try
        {
            // Initial state
            LogoContainer.Opacity = 0;
            LogoContainer.Scale = 0.75;
            AppTitleLabel.Opacity = 0;
            AppTaglineLabel.Opacity = 0;
            SplashProgressBar.Opacity = 0;
            StatusLabel.Opacity = 0;

            // Entrance animation (0 - 800ms)
            await Task.WhenAll(
                LogoContainer.FadeToAsync(1, 700, Easing.CubicOut),
                LogoContainer.ScaleToAsync(1.06, 800, Easing.CubicOut)
            );

            // Settle to natural scale (800 - 1200ms)
            await LogoContainer.ScaleToAsync(1.0, 400, Easing.SinInOut);

            // Reveal typography and progress bar
            await Task.WhenAll(
                AppTitleLabel.FadeToAsync(1, 400),
                AppTaglineLabel.FadeToAsync(1, 400),
                SplashProgressBar.FadeToAsync(1, 400),
                StatusLabel.FadeToAsync(1, 400)
            );

            // Animate progress bar across ~2500ms
            _ = SplashProgressBar.ProgressTo(1.0, 2400, Easing.CubicInOut);

            // Dynamic progress updates (total active duration ~3.6 seconds)
            await Task.Delay(800);
            StatusLabel.Text = "Curating authentic spices & biryanis...";
            
            // Subtle breathing pulse on the logo
            _ = Task.Run(async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LogoContainer.ScaleToAsync(1.03, 600, Easing.SinInOut);
                    await LogoContainer.ScaleToAsync(1.0, 600, Easing.SinInOut);
                });
            });

            await Task.Delay(1000);
            StatusLabel.Text = "Preparing gourmet kitchen studio...";

            await Task.Delay(900);
            StatusLabel.Text = "Welcome to Recipes!";

            await Task.Delay(400);

            // Smooth exit transition
            await this.FadeToAsync(0, 300, Easing.CubicIn);

            // Navigate to main application shell
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var shell = new AppShell();
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = shell;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashPage] Animation exception: {ex.Message}");
            // Fallback safe transition
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var shell = new AppShell();
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = shell;
                }
            });
        }
    }
}
