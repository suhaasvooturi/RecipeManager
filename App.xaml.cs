using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using RecipeManager.Services;

namespace RecipeManager;

public partial class App : Application
{
    private readonly IDatabaseService _databaseService;

    private static void Log(string msg)
    {
        try
        {
            var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log");
            System.IO.File.AppendAllText(path, $"[{System.DateTime.Now:HH:mm:ss.fff}] [MAUI.App] {msg}\r\n");
        }
        catch {}
    }

    public App(IDatabaseService databaseService)
    {
        Log("App ctor entered");
        InitializeComponent();
        _databaseService = databaseService;
        Log("App ctor finished");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Log("CreateWindow entered");
        try
        {
            Task.Run(async () => await _databaseService.InitializeAsync());
            Log("Creating SplashPage");
            var splashPage = new Views.SplashPage();

            Log("Creating Window");
            var win = new Window(splashPage)
            {
                Title = "Recipes"
            };

            win.Created += (s, e) =>
            {
                Log("Window.Created fired");
#if WINDOWS
                try
                {
                    var nativeWindow = win.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (nativeWindow != null)
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                        Log($"Native HWND: {hwnd}");
                        nativeWindow.Activate();
                    }
                }
                catch (System.Exception ex)
                {
                    Log($"Window.Created native HWND error: {ex}");
                }
#endif
            };

            win.Activated += (s, e) =>
            {
                Log("Window.Activated fired");
#if WINDOWS
                try
                {
                    var nativeWindow = win.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (nativeWindow != null)
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                        Log($"Window.Activated Native HWND: {hwnd}");
                    }
                }
                catch (System.Exception ex)
                {
                    Log($"Window.Activated error: {ex}");
                }
#endif
            };

            win.Deactivated += (s, e) => Log("Window.Deactivated fired");
            win.Stopped += (s, e) => Log("Window.Stopped fired");
            win.Destroying += (s, e) => Log("Window.Destroying fired");
            Log("CreateWindow finished");
            return win;
        }
        catch (System.Exception ex)
        {
            Log($"CreateWindow failed: {ex}");
            throw;
        }
    }
}