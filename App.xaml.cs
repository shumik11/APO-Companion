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
            services.AddSingleton<APO.ViewModels.MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            this.SessionEnding += App_SessionEnding;
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<APO.ViewModels.MainViewModel>();
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                mainWindow.DataContext = viewModel;

                viewModel.RequestApoConfigPathAction = mainWindow.PromptForApoConfigPath;
                viewModel.RequestPresetsFolderAction = mainWindow.PromptForPresetsFolder;

                mainWindow.Show();

                await viewModel.InitializeAsync();
                viewModel.PromptForApoConfigPathIfNeeded();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash.txt", ex.ToString());
                MessageBox.Show($"Startup Error: {ex.Message}\nCheck crash.txt for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            try
            {
                var mainWindow = MainWindow as MainWindow;
                mainWindow?.PrepareForClose();
            }
            catch
            {
                // Ignore
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var viewModel = _serviceProvider.GetService<APO.ViewModels.MainViewModel>();
                viewModel?.Dispose();
                _serviceProvider.Dispose();
            }
            catch
            {
                // Ignore errors during exit
            }
            base.OnExit(e);
        }
    }
}
