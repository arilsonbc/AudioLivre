using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AudioLivre;
public class App : Application {
    public override void Initialize() { RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light; Styles.Add(new FluentTheme()); }
    public override void OnFrameworkInitializationCompleted() {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow=new MainWindow();
            if(desktop.Args?.Contains("--smoke-ui")==true)
                DispatcherTimer.RunOnce(()=>desktop.Shutdown(0),TimeSpan.FromSeconds(2));
        }
        base.OnFrameworkInitializationCompleted();
    }
}
public static class Program {
    [STAThread] public static int Main(string[] args) {
        if(args.Length>0 && args[0]=="--convert") {
            try {
                if(args.Length<5) return 2;
                new Engine(AppContext.BaseDirectory).ConvertFile(args[1],args[2],args[3],int.Parse(args[4]),CancellationToken.None,_=>{});
                return 0;
            } catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().StartWithClassicDesktopLifetime(args);
    }
}
