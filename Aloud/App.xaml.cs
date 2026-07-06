using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.Threading.Tasks;
using Velopack;

namespace LectorGlobalApp;

public partial class App : System.Windows.Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _ = UpdateMyApp();
    }

    private static async Task UpdateMyApp()
    {
        try
        {
            // Apunta al repositorio de GitHub donde se subirán las Releases
            var mgr = new UpdateManager("https://github.com/MatiasEtcheverrySoto/lector-global");
            
            // Solo busca actualizaciones si la app está instalada mediante Setup.exe
            if (mgr.IsInstalled)
            {
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await mgr.DownloadUpdatesAsync(newVersion);
                    mgr.ApplyUpdatesAndRestart(newVersion);
                }
            }
        }
        catch
        {
            // Ignorar errores de red si no hay conexión a internet
        }
    }
}
