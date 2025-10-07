using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Hardcodet.Wpf.TaskbarNotification;

namespace APO
{
    public partial class MainWindow : Window
    {
        private const byte TRANSPARENT_ALPHA = 80;
        private const byte OPAQUE_ALPHA = 230;
        private const int MAX_RECENT_PRESETS = 5;
        private const string AppRegistryName = "APO Companion";

        private Color _themeBackgroundColor;
        private Color _transparentColor;
        private Color _opaqueColor;

        private const string DefaultApoConfigPath = @"C:\Program Files\EqualizerAPO\config\config.txt";
        private string? _equalizerApoConfigPath;
        private string? _presetsFolderPath;
        private bool _isProgrammaticSelection = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeThemeColors();
            LoadAndValidateWindowPosition();
            if (!InitializeApoConfigPath()) { Application.Current.Shutdown(); return; }
            LoadSettings();
        }

        private void InitializeThemeColors()
        {
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
        private void ApplyPreset(string presetFileName) { if (string.IsNullOrEmpty(_presetsFolderPath) || string.IsNullOrEmpty(_equalizerApoConfigPath)) return; string fullPresetPath = Path.Combine(_presetsFolderPath, presetFileName); if (!System.IO.File.Exists(fullPresetPath)) return; try { string content = $"Include: {fullPresetPath}"; System.IO.File.WriteAllText(_equalizerApoConfigPath, content); notifyIcon.ToolTipText = $"APO Companion: {Path.GetFileNameWithoutExtension(presetFileName)}"; UpdateRecentPresets(presetFileName); } catch (Exception ex) { MessageBox.Show($"Не удалось применить пресет: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void UpdateRecentPresets(string presetFileName) { var recents = APO.Properties.Settings.Default.RecentPresets; if (recents == null) { recents = new StringCollection(); } if (recents.Contains(presetFileName)) { recents.Remove(presetFileName); } recents.Insert(0, presetFileName); while (recents.Count > MAX_RECENT_PRESETS) { recents.RemoveAt(recents.Count - 1); } APO.Properties.Settings.Default.RecentPresets = recents; APO.Properties.Settings.Default.Save(); }
        private void LoadPresetsFromFolder(string folderPath) { if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return; try { var presetFiles = Directory.GetFiles(folderPath, "*.txt").Select(Path.GetFileName).Where(f => f != null).ToList(); presetsComboBox.ItemsSource = presetFiles; if (presetFiles.Any()) { presetsComboBox.IsEnabled = true; StatusTextBlock.Visibility = Visibility.Collapsed; SyncWithCurrentConfig(presetFiles!); } else { presetsComboBox.IsEnabled = false; StatusTextBlock.Text = "В этой папке пресеты не найдены"; StatusTextBlock.Visibility = Visibility.Visible; } } catch (Exception ex) { MessageBox.Show($"Ошибка при чтении папки с пресетами: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void SyncWithCurrentConfig(System.Collections.Generic.List<string> presetFiles) { if (string.IsNullOrEmpty(_equalizerApoConfigPath) || !System.IO.File.Exists(_equalizerApoConfigPath)) return; try { string currentConfig = System.IO.File.ReadAllText(_equalizerApoConfigPath); string? activePreset = null; foreach (var presetFile in presetFiles) { if (currentConfig.Contains(presetFile)) { activePreset = presetFile; break; } } if (activePreset != null) { _isProgrammaticSelection = true; presetsComboBox.SelectedItem = activePreset; _isProgrammaticSelection = false; } else { presetsComboBox.SelectedItem = null; } } catch { /* Игнорируем ошибки */ } }
        private void PresetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_isProgrammaticSelection && presetsComboBox.SelectedItem is string selectedPreset) { ApplyPreset(selectedPreset); } }
        private bool InitializeApoConfigPath() { string savedPath = APO.Properties.Settings.Default.EqualizerApoConfigPath; if (!string.IsNullOrEmpty(savedPath) && System.IO.File.Exists(savedPath)) { _equalizerApoConfigPath = savedPath; return true; } if (System.IO.File.Exists(DefaultApoConfigPath)) { _equalizerApoConfigPath = DefaultApoConfigPath; return true; } return PromptForApoConfigPath(); }
        private bool PromptForApoConfigPath() { MessageBox.Show("Не удалось автоматически найти файл config.txt от Equalizer APO.\n\nПожалуйста, укажите его расположение.", "Файл конфигурации не найден", MessageBoxButton.OK, MessageBoxImage.Information); var dialog = new CommonOpenFileDialog { Title = "Выберите файл config.txt", IsFolderPicker = false, InitialDirectory = @"C:\Program Files\EqualizerAPO\config" }; dialog.Filters.Add(new CommonFileDialogFilter("Файл конфигурации", "config.txt")); if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { _equalizerApoConfigPath = dialog.FileName; APO.Properties.Settings.Default.EqualizerApoConfigPath = _equalizerApoConfigPath; APO.Properties.Settings.Default.Save(); return true; } return false; }
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) { var dialog = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Выберите папку с вашими .txt пресетами" }; if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { _presetsFolderPath = dialog.FileName; SaveSettings(); LoadPresetsFromFolder(_presetsFolderPath); } }
        #endregion

        #region Window and Tray Management
        private void NotifyIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e) { if (this.IsVisible) { ShowHideMenuItem.Header = "Скрыть виджет"; } else { ShowHideMenuItem.Header = "Показать виджет"; } BuildRecentPresetsMenu(); if (IsInAutorun()) { AutorunMenuItem.Header = "Убрать из автозапуска"; } else { AutorunMenuItem.Header = "Добавить в автозапуск"; } }

        private void AutorunMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsInAutorun()) { SetAutorun(false); notifyIcon.ShowBalloonTip("Автозапуск", "Приложение убрано из автозапуска.", BalloonIcon.Info); }
                else { SetAutorun(true); notifyIcon.ShowBalloonTip("Автозапуск", "Приложение добавлено в автозапуск.", BalloonIcon.Info); }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось изменить настройки автозапуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsInAutorun()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
            {
                return key?.GetValue(AppRegistryName) != null;
            }
        }

        private void SetAutorun(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!)
            {
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        throw new InvalidOperationException("Не удалось определить путь к приложению.");
                    }
                    key.SetValue(AppRegistryName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppRegistryName, false);
                }
            }
        }

        private void BuildRecentPresetsMenu() { var recents = APO.Properties.Settings.Default.RecentPresets; RecentPresetsMenuItem.Items.Clear(); if (recents == null || recents.Count == 0) { RecentPresetsMenuItem.Visibility = Visibility.Collapsed; return; } RecentPresetsMenuItem.Visibility = Visibility.Visible; foreach (var presetName in recents) { var menuItem = new MenuItem { Header = Path.GetFileNameWithoutExtension(presetName) }; string currentPreset = presetName!; menuItem.Click += (s, args) => { _isProgrammaticSelection = true; presetsComboBox.SelectedItem = currentPreset; ApplyPreset(currentPreset); _isProgrammaticSelection = false; }; RecentPresetsMenuItem.Items.Add(menuItem); } }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); } }
        private void Window_Closing(object sender, CancelEventArgs e) { APO.Properties.Settings.Default.WindowTop = this.Top; APO.Properties.Settings.Default.WindowLeft = this.Left; APO.Properties.Settings.Default.Save(); e.Cancel = true; this.Hide(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void NotifyIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e) { if (this.IsVisible) { this.Hide(); } else { this.Show(); this.Activate(); } }
        private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e) { if (this.IsVisible) { this.Hide(); } else { this.Show(); this.Activate(); } }
        private void ResetPositionMenuItem_Click(object sender, RoutedEventArgs e) { double screenWidth = SystemParameters.PrimaryScreenWidth; double screenHeight = SystemParameters.PrimaryScreenHeight; this.Left = (screenWidth / 2) - (this.Width / 2); this.Top = (screenHeight / 2) - (this.Height / 2); this.Show(); this.Activate(); }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) { notifyIcon.Dispose(); Application.Current.Shutdown(); }
        #endregion

        #region Settings Persistence
        private void LoadAndValidateWindowPosition() { double savedTop = APO.Properties.Settings.Default.WindowTop; double savedLeft = APO.Properties.Settings.Default.WindowLeft; bool areCoordinatesValid = double.IsFinite(savedTop) && double.IsFinite(savedLeft); if (areCoordinatesValid) { var screenWidth = SystemParameters.PrimaryScreenWidth; var screenHeight = SystemParameters.PrimaryScreenHeight; if (savedTop > screenHeight - 50 || savedLeft > screenWidth - 50 || savedTop < 0 || savedLeft < 0) { areCoordinatesValid = false; } } if (areCoordinatesValid) { this.Top = savedTop; this.Left = savedLeft; } else { this.Top = 150; this.Left = 150; } }
        private void LoadSettings() { _presetsFolderPath = APO.Properties.Settings.Default.PresetsFolderPath; if (!string.IsNullOrEmpty(_presetsFolderPath)) { LoadPresetsFromFolder(_presetsFolderPath); } }
        private void SaveSettings() { APO.Properties.Settings.Default.PresetsFolderPath = _presetsFolderPath; APO.Properties.Settings.Default.Save(); }
        #endregion
    }
}