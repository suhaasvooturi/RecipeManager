using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RecipeManager.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	private static void Log(string message)
	{
		try
		{
			var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup.log");
			System.IO.File.AppendAllText(path, $"[{System.DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
		}
		catch {}
	}

	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		Log("App() ctor entered");
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			Log($"Unhandled AppDomain exception: {e.ExceptionObject}");
			try { System.IO.File.WriteAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.ExceptionObject?.ToString() ?? "Unknown AppDomain error"); } catch {}
		};
		this.UnhandledException += (s, e) =>
		{
			Log($"Unhandled WinUI exception: {e.Exception?.ToString() ?? e.Message}");
			try { System.IO.File.WriteAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.Exception?.ToString() ?? e.Message); } catch {}
		};

		try
		{
			this.InitializeComponent();
			Log("App InitializeComponent() succeeded");
		}
		catch (System.Exception ex)
		{
			Log($"App InitializeComponent() failed: {ex}");
			throw;
		}
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		Log("OnLaunched() entered");
		try
		{
			base.OnLaunched(args);
			Log("base.OnLaunched() completed successfully");
		}
		catch (System.Exception ex)
		{
			Log($"OnLaunched failed with exception: {ex}");
			throw;
		}
	}

	protected override MauiApp CreateMauiApp()
	{
		Log("CreateMauiApp() called");
		try
		{
			var mauiApp = MauiProgram.CreateMauiApp();
			Log("CreateMauiApp() succeeded");
			return mauiApp;
		}
		catch (System.Exception ex)
		{
			Log($"CreateMauiApp() failed: {ex}");
			throw;
		}
	}
}


