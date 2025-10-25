using APO.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace APO
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPresetService, PresetService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IAutorunService, AutorunService>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow?.Show();
        }
    }
}
