using Hardcodet.Wpf.TaskbarNotification;
using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using APO.Helpers;
using APO.Services;
using APO.ViewModels;

namespace APO
{
    public partial class MainWindow : Window
    {
        private const byte TRANSPARENT_ALPHA = 80;
        private const byte OPAQUE_ALPHA = 220;

        private readonly ISettingsService _settingsService;

        private Color _themeBackgroundColor;
        private Color _transparentColor;
        private Color _opaqueColor;

        public MainWindow(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            LoadAndApplyTheme();
            InitializeComponent();
            InitializeThemeColors();
            LoadAndValidateWindowPosition();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.OnPresetApplied += (presetName) =>
                {
                    notifyIcon.ToolTipText = $"{FindResource("WindowTitle")}: {Path.GetFileNameWithoutExtension(presetName)}";
                };

                viewModel.OnShowNotification += (title, text, icon) =>
                {
                    notifyIcon.ShowBalloonTip(title, text, icon);
                };

                viewModel.OnThemeChanged += (theme) =>
                {
                    ApplyTheme(theme);
                };
            }
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

        #region Dialogs (Requested by VM)
        public bool PromptForApoConfigPath()
        {
            MessageBox.Show("Не удалось автоматически найти файл config.txt от Equalizer APO.\n\nПожалуйста, укажите его расположение.", "Файл конфигурации не найден", MessageBoxButton.OK, MessageBoxImage.Information);
            var dialog = new CommonOpenFileDialog
            {
                Title = "Выберите файл config.txt",
                IsFolderPicker = false,
                InitialDirectory = @"C:\Program Files\EqualizerAPO\config"
            };
            dialog.Filters.Add(new CommonFileDialogFilter("Файл конфигурации", "config.txt"));
            
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    viewModel.EqualizerApoConfigPath = dialog.FileName;
                    _settingsService.SaveEqualizerApoConfigPath(dialog.FileName);
                }
                return true;
            }
            return false;
        }

        public string? PromptForPresetsFolder()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Выберите папку с вашими .txt пресетами"
            };
            
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }
            return null;
        }
        #endregion

        #region Widget Animation
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (WidgetCard == null || WidgetCard.Background == null) return;
            ColorAnimation animation = new ColorAnimation(_opaqueColor, TimeSpan.FromMilliseconds(200));
            WidgetCard.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (WidgetCard == null || WidgetCard.Background == null) return;
            ColorAnimation animation = new ColorAnimation(_transparentColor, TimeSpan.FromMilliseconds(200));
            WidgetCard.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        #endregion

        #region Window and Tray Handlers
        private bool _isExplicitClose = false;

        public void PrepareForClose()
        {
            _isExplicitClose = true;
        }

        private void ShowAndFocus()
        {
            this.Show();
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                _settingsService.SaveWindowPosition(this.Top, this.Left);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_isExplicitClose)
            {
                return; // Let the window close normally
            }

            _settingsService.SaveWindowPosition(this.Top, this.Left);
            e.Cancel = true;
            this.Hide();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.Hide();

        private void NotifyIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                ShowAndFocus();
            }
        }

        private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                ShowAndFocus();
            }
        }

        private void ResetPositionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (screenWidth / 2) - (this.Width / 2);
            this.Top = (screenHeight / 2) - (this.Height / 2);
            ShowAndFocus();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            PrepareForClose();
            notifyIcon.Dispose();
            
            var viewModel = DataContext as MainViewModel;
            viewModel?.Dispose();

            Application.Current.Shutdown();
        }
        #endregion

        #region Theme Switching
        private void LoadAndApplyTheme()
        {
            string theme = _settingsService.LoadTheme();
            ApplyTheme(theme);
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

        #region Settings Window Placement
        private void LoadAndValidateWindowPosition()
        {
            var (savedTop, savedLeft) = _settingsService.LoadWindowPosition();

            // If coordinates are 0 (first launch), place in bottom-right corner of working area
            if (savedTop == 0 && savedLeft == 0)
            {
                var workingArea = SystemParameters.WorkArea;
                this.Left = workingArea.Right - this.Width - 15;
                this.Top = workingArea.Bottom - this.Height - 15;
                return;
            }

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
                var workingArea = SystemParameters.WorkArea;
                this.Left = workingArea.Right - this.Width - 15;
                this.Top = workingArea.Bottom - this.Height - 15;
            }
        }
        #endregion
    }
}