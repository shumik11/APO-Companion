using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace APO
{
    public partial class MainWindow : Window
    {
        private const byte TRANSPARENT_ALPHA = 80;
        private const byte OPAQUE_ALPHA = 230;

        private Color _themeBackgroundColor;
        private Color _transparentColor;
        private Color _opaqueColor;

        private const string DefaultApoConfigPath = @"C:\Program Files\EqualizerAPO\config\config.txt";
        private string? _equalizerApoConfigPath;
        private string? _presetsFolderPath;
        private bool _isLocked = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeThemeColors();
            this.Top = 150;
            this.Left = 150;
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
        private void LockMenuItem_Click(object sender, RoutedEventArgs e) { _isLocked = !_isLocked; if (_isLocked) { DesktopWidgetHelper.EnableClickThrough(this); LockMenuItem.Header = "Разблокировать"; } else { DesktopWidgetHelper.DisableClickThrough(this); LockMenuItem.Header = "Блокировать"; } }
        private void Window_MouseEnter(object sender, MouseEventArgs e) { ColorAnimation animation = new ColorAnimation(_opaqueColor, TimeSpan.FromMilliseconds(200)); WidgetCard.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation); }
        private void Window_MouseLeave(object sender, MouseEventArgs e) { ColorAnimation animation = new ColorAnimation(_transparentColor, TimeSpan.FromMilliseconds(200)); WidgetCard.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation); }
        #endregion

        #region Core Logic

        // ИЗМЕНЕННЫЙ МЕТОД: Теперь он управляет состоянием интерфейса
        private void LoadPresetsFromFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;
            try
            {
                var presetFiles = Directory.GetFiles(folderPath, "*.txt").Select(Path.GetFileName).Where(f => f != null).ToList();
                presetsComboBox.ItemsSource = presetFiles;

                if (presetFiles.Any())
                {
                    // Если пресеты найдены
                    presetsComboBox.IsEnabled = true;
                    StatusTextBlock.Visibility = Visibility.Collapsed; // Скрываем подсказку
                    presetsComboBox.SelectedIndex = 0; // Автоматически выбираем первый
                }
                else
                {
                    // Если папка пуста
                    presetsComboBox.IsEnabled = false;
                    StatusTextBlock.Text = "В этой папке пресеты не найдены";
                    StatusTextBlock.Visibility = Visibility.Visible; // Показываем сообщение
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении папки с пресетами: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool InitializeApoConfigPath() { string savedPath = APO.Properties.Settings.Default.EqualizerApoConfigPath; if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath)) { _equalizerApoConfigPath = savedPath; return true; } if (File.Exists(DefaultApoConfigPath)) { _equalizerApoConfigPath = DefaultApoConfigPath; return true; } return PromptForApoConfigPath(); }
        private bool PromptForApoConfigPath() { MessageBox.Show("Не удалось автоматически найти файл config.txt от Equalizer APO.\n\nПожалуйста, укажите его расположение.", "Файл конфигурации не найден", MessageBoxButton.OK, MessageBoxImage.Information); var dialog = new CommonOpenFileDialog { Title = "Выберите файл config.txt", IsFolderPicker = false, InitialDirectory = @"C:\Program Files\EqualizerAPO\config" }; dialog.Filters.Add(new CommonFileDialogFilter("Файл конфигурации", "config.txt")); if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { _equalizerApoConfigPath = dialog.FileName; APO.Properties.Settings.Default.EqualizerApoConfigPath = _equalizerApoConfigPath; APO.Properties.Settings.Default.Save(); return true; } return false; }
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) { var dialog = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Выберите папку с вашими .txt пресетами" }; if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { _presetsFolderPath = dialog.FileName; SaveSettings(); LoadPresetsFromFolder(_presetsFolderPath); } }
        private void PresetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (presetsComboBox.SelectedItem is string selectedPreset) { ApplyPreset(selectedPreset); } }
        private void ApplyPreset(string presetFileName) { if (string.IsNullOrEmpty(_presetsFolderPath) || string.IsNullOrEmpty(_equalizerApoConfigPath)) return; string fullPresetPath = Path.Combine(_presetsFolderPath, presetFileName); if (!File.Exists(fullPresetPath)) return; try { string content = $"Include: {fullPresetPath}"; File.WriteAllText(_equalizerApoConfigPath, content); notifyIcon.ToolTipText = $"APO Companion: {Path.GetFileNameWithoutExtension(presetFileName)}"; } catch (Exception ex) { MessageBox.Show($"Не удалось применить пресет: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); } }
        #endregion

        #region Window and Tray Management
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (!_isLocked && e.ButtonState == MouseButtonState.Pressed) { DragMove(); } }
        private void Window_Closing(object sender, CancelEventArgs e) { e.Cancel = true; this.Hide(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void NotifyIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e) { if (this.IsVisible) { this.Hide(); } else { this.Show(); this.Activate(); } }
        private void NotifyIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e) { if (this.IsVisible) { ShowHideMenuItem.Header = "Скрыть виджет"; } else { ShowHideMenuItem.Header = "Показать виджет"; } }
        private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e) { if (this.IsVisible) { this.Hide(); } else { this.Show(); this.Activate(); } }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) { notifyIcon.Dispose(); Application.Current.Shutdown(); }
        #endregion

        #region Settings Persistence
        private void LoadSettings() { _presetsFolderPath = APO.Properties.Settings.Default.PresetsFolderPath; if (!string.IsNullOrEmpty(_presetsFolderPath)) { LoadPresetsFromFolder(_presetsFolderPath); } }
        private void SaveSettings() { APO.Properties.Settings.Default.PresetsFolderPath = _presetsFolderPath; APO.Properties.Settings.Default.Save(); }
        #endregion
    }
}