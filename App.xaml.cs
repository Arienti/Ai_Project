using Ai_Project.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Ai_Project
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register your services
                    services.AddSingleton<OllamaService>();
                    // Register the main window
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (AppHost == null)
                throw new InvalidOperationException("AppHost is not initialized.");

            await AppHost.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost == null)
                throw new InvalidOperationException("AppHost is not initialized.");
            await AppHost.StopAsync();
            base.OnExit(e);
        }
    }

}
