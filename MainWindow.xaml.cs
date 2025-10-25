using Hardcodet.Wpf.TaskbarNotification;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using APO.Services;

namespace APO
{
    public partial class MainWindow : Window
    {
        private const byte TRANSPARENT_ALPHA = 80;
        private const byte OPAQUE_ALPHA = 230;

        private readonly IPresetService _presetService;
        private readonly ISettingsService _settingsService;
        private readonly IAutorunService _autorunService;

        private Color _themeBackgroundColor;
        private Color _transparentColor;
        private Color _opaqueColor;

        private const string DefaultApoConfigPath = @"C:\Program Files\EqualizerAPO\config\config.txt";
        private string? _equalizerApoConfigPath;
        private string? _presetsFolderPath;
        private bool _isProgrammaticSelection = false;

        private FileSystemWatcher? _presetsFolderWatcher;
        private Timer? _debounceTimer;

        public MainWindow(IPresetService presetService, ISettingsService settingsService, IAutorunService autorunService)
        {
            _presetService = presetService;
            _settingsService = settingsService;
            _autorunService = autorunService;

            LoadAndApplyLanguage();
            LoadAndApplyTheme();
            InitializeComponent();
            InitializeThemeColors();
            LoadAndValidateWindowPosition();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!InitializeApoConfigPath()) { Application.Current.Shutdown(); return; }
            await LoadSettingsAsync();
        }

        private void InitializeThemeColors()
        {
            if (WidgetCard == null) return;
            var themeBrush = (SolidColorBrush)this.FindResource("MaterialDesignPaper");
            _themeBackgroundColor = themeBrush.Color;
            _transparentColor = Color.FromArgb(TRANSPARENT_ALPHA, _themeBackgroundColor.R, _themeBackgroundColor.G, _themeBackgroundColor.B);
            _opaqueColor = Color.FromArgb(OPAQUE_ALPHA, _themeBackgroundColor.R, _themeBackgroundColor.G, _themeBackgroundColor.B);
            WidgetCard.Background = new SolidColorBrush(_transparentColor);
        }

        #region Widget Specific Logic
        private void Window_MouseEnter(object sender, MouseEventArgs e) { ColorAnimation animation = new ColorAnimation(_opaqueColor, TimeSpan.FromMilliseconds(200)); WidgetCard.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation); }
        private void Window_MouseLeave(object sender, MouseEventArgs e) { ColorAnimation animation = new ColorAnimation(_transparentColor, TimeSpan.FromMilliseconds(200)); WidgetCard.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation); }
        #endregion

        #region Core Logic
        private async Task ApplyPresetAsync(string presetFileName)
        {
            if (_presetsFolderPath == null || _equalizerApoConfigPath == null) return;
            try
            {
                await _presetService.ApplyPresetAsync(_presetsFolderPath, _equalizerApoConfigPath, presetFileName);
                notifyIcon.ToolTipText = $"{FindResource("WindowTitle")}: {Path.GetFileNameWithoutExtension(presetFileName)}";
                _settingsService.UpdateRecentPresets(presetFileName);
            }
            catch (Exception ex)
            {
                notifyIcon.ShowBalloonTip((string)FindResource("Error"), $"{(string)FindResource("PresetApplyError")}: {ex.Message}", BalloonIcon.Error);
            }
        }

        private async Task LoadPresetsFromFolderAsync(string folderPath)
        {
            presetsComboBox.IsEnabled = false;
            SelectFolderButton.IsEnabled = false;
            StatusTextBlock.Text = (string)FindResource("Loading");
            StatusTextBlock.Visibility = Visibility.Visible;

            try
            {
                var presetFiles = await _presetService.LoadPresetsFromFolderAsync(folderPath);
                presetsComboBox.ItemsSource = presetFiles;

                if (presetFiles.Any())
                {
                    presetsComboBox.IsEnabled = true;
                    StatusTextBlock.Visibility = Visibility.Collapsed;
                    if (_equalizerApoConfigPath != null)
                    {
                        var activePreset = await _presetService.SyncWithCurrentConfigAsync(_equalizerApoConfigPath, presetFiles);
                        if (activePreset != null)
                        {
                            try
                            {
                                _isProgrammaticSelection = true;
                                presetsComboBox.SelectedItem = activePreset;
                            }
                            finally
                            {
                                _isProgrammaticSelection = false;
                            }
                        }
                        else
                        {
                            presetsComboBox.SelectedItem = null;
                        }
                    }
                }
                else
                {
                    presetsComboBox.IsEnabled = false;
                    StatusTextBlock.Text = (string)FindResource("PresetsNotFound");
                    StatusTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                presetsComboBox.ItemsSource = null;
                presetsComboBox.IsEnabled = false;
                StatusTextBlock.Text = (string)FindResource("PresetReadError");
                StatusTextBlock.Visibility = Visibility.Visible;
                notifyIcon.ShowBalloonTip((string)FindResource("Error"), $"{ex.Message}", BalloonIcon.Error);
            }
            finally
            {
                SelectFolderButton.IsEnabled = true;
            }
        }
        private async void PresetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_isProgrammaticSelection && presetsComboBox.SelectedItem is string selectedPreset) { await ApplyPresetAsync(selectedPreset); } }
        private bool InitializeApoConfigPath() { string savedPath = _settingsService.LoadEqualizerApoConfigPath(); if (!string.IsNullOrEmpty(savedPath) && System.IO.File.Exists(savedPath)) { _equalizerApoConfigPath = savedPath; return true; } if (System.IO.File.Exists(DefaultApoConfigPath)) { _equalizerApoConfigPath = DefaultApoConfigPath; return true; } return PromptForApoConfigPath(); }
        private bool PromptForApoConfigPath() { MessageBox.Show("Не удалось автоматически найти файл config.txt от Equalizer APO.\n\nПожалуйста, укажите его расположение.", "Файл конфигурации не найден", MessageBoxButton.OK, MessageBoxImage.Information); var dialog = new CommonOpenFileDialog { Title = "Выберите файл config.txt", IsFolderPicker = false, InitialDirectory = @"C:\Program Files\EqualizerAPO\config" }; dialog.Filters.Add(new CommonFileDialogFilter("Файл конфигурации", "config.txt")); if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { _equalizerApoConfigPath = dialog.FileName; _settingsService.SaveEqualizerApoConfigPath(_equalizerApoConfigPath); return true; } return false; }
        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e) { var dialog = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Выберите папку с вашими .txt пресетами" }; if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { _presetsFolderPath = dialog.FileName; if(_presetsFolderPath != null) SaveSettings(); await LoadPresetsFromFolderAsync(_presetsFolderPath); InitializeFileSystemWatcher(_presetsFolderPath); } }
        private void InitializeFileSystemWatcher(string path) { _presetsFolderWatcher?.Dispose(); _presetsFolderWatcher = new FileSystemWatcher(path) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite, Filter = Constants.PresetsFileFilter, EnableRaisingEvents = true }; _presetsFolderWatcher.Changed += OnPresetsFolderChanged; _presetsFolderWatcher.Created += OnPresetsFolderChanged; _presetsFolderWatcher.Deleted += OnPresetsFolderChanged; _presetsFolderWatcher.Renamed += OnPresetsFolderChanged; }
        private void OnPresetsFolderChanged(object sender, FileSystemEventArgs e) { DebounceFileSystemWatcher(500); }
        private void DebounceFileSystemWatcher(int interval) { _debounceTimer?.Dispose(); _debounceTimer = new Timer(async _ => { await Dispatcher.InvokeAsync(async () => { if (!string.IsNullOrEmpty(_presetsFolderPath)) { await LoadPresetsFromFolderAsync(_presetsFolderPath); } }); }, null, interval, Timeout.Infinite); }
        #endregion

        #region Window and Tray Management
        private void NotifyIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e) { if (this.IsVisible) { ShowHideMenuItem.Header = FindResource("HideWidget"); } else { ShowHideMenuItem.Header = FindResource("ShowWidget"); } BuildRecentPresetsMenu(); if (_autorunService.IsInAutorun()) { AutorunMenuItem.Header = FindResource("RemoveFromAutorun"); } else { AutorunMenuItem.Header = FindResource("AddToAutorun"); } }
        private void AutorunMenuItem_Click(object sender, RoutedEventArgs e) { try { if (_autorunService.IsInAutorun()) { _autorunService.SetAutorun(false); notifyIcon.ShowBalloonTip((string)FindResource("AutorunRemovedBlob"), (string)FindResource("AutorunRemoved"), BalloonIcon.Info); } else { _autorunService.SetAutorun(true); notifyIcon.ShowBalloonTip((string)FindResource("AutorunAddedBlob"), (string)FindResource("AutorunAdded"), BalloonIcon.Info); } } catch (Exception ex) { notifyIcon.ShowBalloonTip((string)FindResource("Error"), $"{(string)FindResource("AutorunError")}: {ex.Message}", BalloonIcon.Error); } }
        private void BuildRecentPresetsMenu() { var recents = _settingsService.LoadRecentPresets(); RecentPresetsMenuItem.Items.Clear(); if (recents == null || recents.Count == 0) { RecentPresetsMenuItem.Visibility = Visibility.Collapsed; return; } RecentPresetsMenuItem.Visibility = Visibility.Visible; foreach (var presetName in recents) { var menuItem = new MenuItem { Header = Path.GetFileNameWithoutExtension(presetName) }; string currentPreset = presetName!; menuItem.Click += async (s, args) => { try { _isProgrammaticSelection = true; presetsComboBox.SelectedItem = currentPreset; await ApplyPresetAsync(currentPreset); } finally { _isProgrammaticSelection = false; } }; RecentPresetsMenuItem.Items.Add(menuItem); } }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); } }
        private void Window_Closing(object sender, CancelEventArgs e) { _settingsService.SaveWindowPosition(this.Top, this.Left); e.Cancel = true; this.Hide(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void NotifyIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e) { if (this.IsVisible) { this.Hide(); } else { this.Show(); this.Activate(); } }
        private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e) { if (this.IsVisible) { this.Hide(); } else { this.Show(); this.Activate(); } }
        private void ResetPositionMenuItem_Click(object sender, RoutedEventArgs e) { double screenWidth = SystemParameters.PrimaryScreenWidth; double screenHeight = SystemParameters.PrimaryScreenHeight; this.Left = (screenWidth / 2) - (this.Width / 2); this.Top = (screenHeight / 2) - (this.Height / 2); this.Show(); this.Activate(); }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) { _debounceTimer?.Dispose(); _presetsFolderWatcher?.Dispose(); notifyIcon.Dispose(); Application.Current.Shutdown(); }

        private void LoadAndApplyLanguage() { string lang = _settingsService.LoadLanguage(); if (string.IsNullOrEmpty(lang)) { lang = CultureInfo.CurrentUICulture.Name; } SwitchLanguage(lang); }
        private void LanguageMenuItem_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem menuItem && menuItem.Tag is string lang) { SwitchLanguage(lang); } }
        private void SwitchLanguage(string langCode) { _settingsService.SaveLanguage(langCode); var stringResources = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == null && d.MergedDictionaries.Any()); if (stringResources == null) { stringResources = new ResourceDictionary(); Application.Current.Resources.MergedDictionaries.Add(stringResources); } var newLangDict = new ResourceDictionary(); try { newLangDict.Source = new Uri($"Strings\\{langCode}.xaml", UriKind.Relative); } catch { newLangDict.Source = new Uri("Strings\\en-US.xaml", UriKind.Relative); } stringResources.MergedDictionaries.Clear(); stringResources.MergedDictionaries.Add(newLangDict); Thread.CurrentThread.CurrentUICulture = new CultureInfo(langCode); }

        private void LoadAndApplyTheme()
        {
            string theme = _settingsService.LoadTheme();
            ApplyTheme(theme);
        }

        private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string theme)
            {
                ApplyTheme(theme);
                _settingsService.SaveTheme(theme);
            }
        }

        private void ApplyTheme(string themeName)
        {
            var themeDictionary = Application.Current.Resources.MergedDictionaries
                .OfType<BundledTheme>()
                .FirstOrDefault();

            if (themeDictionary == null) return;

            switch (themeName)
            {
                case "Dark":
                    themeDictionary.BaseTheme = BaseTheme.Dark;
                    break;
                default: // Light
                    themeDictionary.BaseTheme = BaseTheme.Light;
                    break;
            }

            if (this.IsLoaded)
            {
                InitializeThemeColors();
            }
        }
        #endregion

        #region Settings Persistence
        private void LoadAndValidateWindowPosition()
        {
            var (savedTop, savedLeft) = _settingsService.LoadWindowPosition();

            bool areCoordinatesValid = double.IsFinite(savedTop) && double.IsFinite(savedLeft) &&
                                     savedTop >= SystemParameters.VirtualScreenTop &&
                                     savedLeft >= SystemParameters.VirtualScreenLeft &&
                                     (savedLeft + this.Width) <= (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth) &&
                                     (savedTop + this.Height) <= (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight);

            if (areCoordinatesValid)
            {
                this.Top = savedTop;
                this.Left = savedLeft;
            }
            else
            {
                // Если сохраненные координаты некорректны, центрируем окно
                this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
                this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            }
        }
        private async Task LoadSettingsAsync() { _presetsFolderPath = _settingsService.LoadPresetsFolderPath(); if (!string.IsNullOrEmpty(_presetsFolderPath)) { await LoadPresetsFromFolderAsync(_presetsFolderPath); InitializeFileSystemWatcher(_presetsFolderPath); } }
        private void SaveSettings() { _settingsService.SavePresetsFolderPath(_presetsFolderPath); }
        #endregion
    }
}