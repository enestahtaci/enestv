using System.Windows;
using EnesTV.Core.Data;
using EnesTV.Core.Services;

namespace EnesTV.App;

public partial class App : Application
{
    public static string DataRoot { get; private set; } = Environment.GetEnvironmentVariable("ENESTV_DATA_DIR") is { Length: > 0 } custom ? System.IO.Path.GetFullPath(custom) : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "enestv");
    public static SafeLog Log { get; private set; } = new(System.IO.Path.Combine(DataRoot, "logs"));
    public static LibraryStore Store { get; private set; } = new(System.IO.Path.Combine(DataRoot, "library.db"));
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) => { Log.Write("ui.unhandled", args.Exception); MessageBox.Show("İşlem tamamlanamadı. Lütfen tekrar deneyin.", "enestv"); args.Handled = true; };
        TaskScheduler.UnobservedTaskException += (_, args) => { Log.Write("task.unobserved", args.Exception); args.SetObserved(); };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Write("app.fatal", args.ExceptionObject as Exception);
        try
        {
            Log.Write("app.startup");
            await Store.InitializeAsync();
            LibVLCSharp.Shared.Core.Initialize();
            var window = new MainWindow(); MainWindow = window; window.Show();
        }
        catch (Exception ex) { Log.Write("app.startup.failed", ex); MessageBox.Show("enestv başlatılamadı. Uygulama dosyalarını ve veri klasörü izinlerini kontrol edin.", "enestv"); Shutdown(1); }
    }
}
