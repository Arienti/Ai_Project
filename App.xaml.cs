using Ai_Project.Business;
using Ai_Project.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Ai_Project
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; } = null!;

        public App()
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register your services
                    services.AddSingleton<OllamaService>();
                    services.AddSingleton<TopicBusiness>();
                    // Register the main window
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e); // first

            if (AppHost == null)
                throw new InvalidOperationException("AppHost is not initialized.");

            await AppHost.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
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
