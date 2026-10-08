using System;
using System.IO;

namespace RecipeManager.WinUI;

public static class Program
{
    private static void Log(string msg)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] [Main] {msg}\r\n");
        }
        catch {}
    }

    [STAThread]
    static void Main(string[] args)
    {
        Log("Program.Main entered");
        try
        {
            Log("Calling ComWrappersSupport.InitializeComWrappers()");
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Log("Calling Application.Start()");
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                Log("Application.Start callback invoked!");
                try
                {
                    var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                    System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                    Log("Instantiating new App()");
                    new App();
                    Log("new App() instantiated");
                }
                catch (Exception ex)
                {
                    Log($"Exception in Start callback: {ex}");
                    throw;
                }
            });
            Log("Application.Start completed normally");
        }
        catch (Exception ex)
        {
            Log($"Unhandled exception in Main: {ex}");
        }
    }
}
