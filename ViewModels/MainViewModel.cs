using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using APO.Services;

namespace APO.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IPresetService _presetService;
        private readonly ISettingsService _settingsService;
        private readonly IAutorunService _autorunService;

        private FileSystemWatcher? _presetsFolderWatcher;
        private Timer? _debounceTimer;
        private readonly object _timerLock = new();

        [ObservableProperty]
        private List<string> _presets = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecentPresetsList))]
        private string? _selectedPreset;

        [ObservableProperty]
        private string _statusText = string.Empty;

        [ObservableProperty]
        private bool _isStatusVisible = true;

        [ObservableProperty]
        private bool _isPresetsEnabled = false;

        [ObservableProperty]
        private bool _isSelectFolderEnabled = true;

        [ObservableProperty]
        private string? _presetsFolderPath;

        [ObservableProperty]
        private string? _equalizerApoConfigPath;

        [ObservableProperty]
        private bool _isInAutorun;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoWriteAccess))]
        private bool _hasWriteAccess = true;

        public bool HasNoWriteAccess => !HasWriteAccess;

        [ObservableProperty]
        private string _languageCode = "en-US";

        [ObservableProperty]
        private string _themeName = "Light";

        [ObservableProperty]
        private ObservableCollection<string> _recentPresets = new();

        public List<string> RecentPresetsList => RecentPresets.ToList();

        public List<string> RecentPresetsTop3 => RecentPresets.Take(3).ToList();

        public bool HasRecentPresets => RecentPresets.Any();

        public bool IsPresetActive => !string.IsNullOrEmpty(SelectedPreset);

        private bool _isProgrammaticSelection = false;

        public MainViewModel(IPresetService presetService, ISettingsService settingsService, IAutorunService autorunService)
        {
            _presetService = presetService;
            _settingsService = settingsService;
            _autorunService = autorunService;

            LoadSettings();
            InitializeApoConfigPath();
            IsInAutorun = _autorunService.IsInAutorun();
        }

        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(PresetsFolderPath))
            {
                await LoadPresetsFromFolderAsync(PresetsFolderPath);
                InitializeFileSystemWatcher(PresetsFolderPath);
            }
            await CheckWriteAccessAsync();
        }

        public async Task CheckWriteAccessAsync()
        {
            if (!string.IsNullOrEmpty(EqualizerApoConfigPath))
            {
                HasWriteAccess = await _presetService.HasWriteAccessAsync(EqualizerApoConfigPath);
            }
            else
            {
                HasWriteAccess = false;
            }
        }

        private void InitializeApoConfigPath()
        {
            string savedPath = _settingsService.LoadEqualizerApoConfigPath();
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                EqualizerApoConfigPath = savedPath;
                return;
            }

            const string defaultApoConfigPath = @"C:\Program Files\EqualizerAPO\config\config.txt";
            if (File.Exists(defaultApoConfigPath))
            {
                EqualizerApoConfigPath = defaultApoConfigPath;
                return;
            }
        }

        public Func<bool>? RequestApoConfigPathAction { get; set; }
        public Func<string?>? RequestPresetsFolderAction { get; set; }

        public void PromptForApoConfigPathIfNeeded()
        {
            if (string.IsNullOrEmpty(EqualizerApoConfigPath))
            {
                bool success = RequestApoConfigPathAction?.Invoke() ?? false;
                if (!success)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        public async Task LoadPresetsFromFolderAsync(string folderPath)
        {
            IsPresetsEnabled = false;
            IsSelectFolderEnabled = false;
            StatusText = GetResourceString("Loading");
            IsStatusVisible = true;

            try
            {
                var presetFiles = await _presetService.LoadPresetsFromFolderAsync(folderPath);
                Presets = presetFiles;

                if (presetFiles.Any())
                {
                    IsPresetsEnabled = true;
                    IsStatusVisible = false;
                    if (EqualizerApoConfigPath != null)
                    {
                        var activePreset = await _presetService.SyncWithCurrentConfigAsync(EqualizerApoConfigPath, presetFiles);
                        _isProgrammaticSelection = true;
                        SelectedPreset = activePreset;
                        _isProgrammaticSelection = false;
                    }
                }
                else
                {
                    IsPresetsEnabled = false;
                    StatusText = GetResourceString("PresetsNotFound");
                    IsStatusVisible = true;
                }
            }
            catch (Exception ex)
            {
                Presets = new List<string>();
                IsPresetsEnabled = false;
                StatusText = GetResourceString("PresetReadError");
                IsStatusVisible = true;
                ShowBalloonTip(GetResourceString("Error"), ex.Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
            finally
            {
                IsSelectFolderEnabled = true;
            }
        }

        [RelayCommand]
        public async Task ApplyPresetAsync(string? presetFileName)
        {
            if (string.IsNullOrEmpty(presetFileName) || string.IsNullOrEmpty(PresetsFolderPath) || string.IsNullOrEmpty(EqualizerApoConfigPath))
                return;

            try
            {
                await _presetService.ApplyPresetAsync(PresetsFolderPath, EqualizerApoConfigPath, presetFileName);
                _settingsService.UpdateRecentPresets(presetFileName);
                LoadRecentPresetsList();

                _isProgrammaticSelection = true;
                SelectedPreset = presetFileName;
                _isProgrammaticSelection = false;

                OnPresetApplied?.Invoke(presetFileName);
            }
            catch (Exception ex)
            {
                ShowBalloonTip(GetResourceString("Error"), $"{GetResourceString("PresetApplyError")}: {ex.Message}", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                if (EqualizerApoConfigPath != null && PresetsFolderPath != null)
                {
                    var activePreset = await _presetService.SyncWithCurrentConfigAsync(EqualizerApoConfigPath, Presets);
                    _isProgrammaticSelection = true;
                    SelectedPreset = activePreset;
                    _isProgrammaticSelection = false;
                }
            }
        }

        public event Action<string>? OnPresetApplied;
        public event Action<string, string, Hardcodet.Wpf.TaskbarNotification.BalloonIcon>? OnShowNotification;

        private void ShowBalloonTip(string title, string text, Hardcodet.Wpf.TaskbarNotification.BalloonIcon icon)
        {
            OnShowNotification?.Invoke(title, text, icon);
        }

        [RelayCommand]
        private async Task SelectFolderAsync()
        {
            var folder = RequestPresetsFolderAction?.Invoke();
            if (!string.IsNullOrEmpty(folder))
            {
                PresetsFolderPath = folder;
                _settingsService.SavePresetsFolderPath(folder);
                await LoadPresetsFromFolderAsync(folder);
                InitializeFileSystemWatcher(folder);
            }
        }

        [RelayCommand]
        private void ToggleAutorun()
        {
            try
            {
                if (IsInAutorun)
                {
                    _autorunService.SetAutorun(false);
                    IsInAutorun = false;
                    ShowBalloonTip(GetResourceString("AutorunRemovedBlob"), GetResourceString("AutorunRemoved"), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
                else
                {
                    _autorunService.SetAutorun(true);
                    IsInAutorun = true;
                    ShowBalloonTip(GetResourceString("AutorunAddedBlob"), GetResourceString("AutorunAdded"), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
            }
            catch (Exception ex)
            {
                ShowBalloonTip(GetResourceString("Error"), $"{GetResourceString("AutorunError")}: {ex.Message}", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
        }

        [RelayCommand]
        private void ChangeLanguage(string langCode)
        {
            LanguageCode = langCode;
            _settingsService.SaveLanguage(langCode);
            SwitchLanguageResources(langCode);
            if (IsStatusVisible)
            {
                if (Presets.Any())
                    StatusText = GetResourceString("Loading");
                else if (!string.IsNullOrEmpty(PresetsFolderPath))
                    StatusText = GetResourceString("PresetsNotFound");
                else
                    StatusText = GetResourceString("StatusInitial");
            }
        }

        [RelayCommand]
        private void ChangeTheme(string theme)
        {
            ThemeName = theme;
            _settingsService.SaveTheme(theme);
            OnThemeChanged?.Invoke(theme);
        }

        public event Action<string>? OnThemeChanged;

        private void LoadSettings()
        {
            PresetsFolderPath = _settingsService.LoadPresetsFolderPath();
            LanguageCode = NormalizeLanguageCode(_settingsService.LoadLanguage());
            if (string.IsNullOrEmpty(LanguageCode))
            {
                LanguageCode = NormalizeLanguageCode(System.Globalization.CultureInfo.CurrentUICulture.Name);
            }
            SwitchLanguageResources(LanguageCode);
            ThemeName = _settingsService.LoadTheme();
            if (string.IsNullOrEmpty(ThemeName))
            {
                ThemeName = "Light";
            }
            LoadRecentPresetsList();
        }

        private void LoadRecentPresetsList()
        {
            var recents = _settingsService.LoadRecentPresets();
            RecentPresets.Clear();
            if (recents != null)
            {
                foreach (var preset in recents)
                {
                    if (preset != null)
                    {
                        RecentPresets.Add(preset);
                    }
                }
            }
            OnPropertyChanged(nameof(RecentPresetsList));
            OnPropertyChanged(nameof(RecentPresetsTop3));
            OnPropertyChanged(nameof(HasRecentPresets));
        }

        private void InitializeFileSystemWatcher(string path)
        {
            _presetsFolderWatcher?.Dispose();
            try
            {
                if (Directory.Exists(path))
                {
                    _presetsFolderWatcher = new FileSystemWatcher(path)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                        Filter = Constants.PresetsFileFilter,
                        EnableRaisingEvents = true
                    };
                    _presetsFolderWatcher.Changed += OnPresetsFolderChanged;
                    _presetsFolderWatcher.Created += OnPresetsFolderChanged;
                    _presetsFolderWatcher.Deleted += OnPresetsFolderChanged;
                    _presetsFolderWatcher.Renamed += OnPresetsFolderChanged;
                }
            }
            catch (Exception ex)
            {
                ShowBalloonTip(GetResourceString("Error"), $"Watcher error: {ex.Message}", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
            }
        }

        private void OnPresetsFolderChanged(object sender, FileSystemEventArgs e)
        {
            DebounceFileSystemWatcher(500);
        }

        private void DebounceFileSystemWatcher(int interval)
        {
            lock (_timerLock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(async _ =>
                {
                    if (!string.IsNullOrEmpty(PresetsFolderPath))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await LoadPresetsFromFolderAsync(PresetsFolderPath);
                        });
                    }
                }, null, interval, Timeout.Infinite);
            }
        }

        private string GetResourceString(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        private string NormalizeLanguageCode(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return "en-US";
            if (langCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase)) return "ru-RU";
            return "en-US";
        }

        private void SwitchLanguageResources(string langCode)
        {
            string normalized = NormalizeLanguageCode(langCode);
            var stringResources = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source == null && d.MergedDictionaries.Any());

            if (stringResources == null)
            {
                stringResources = new ResourceDictionary();
                Application.Current.Resources.MergedDictionaries.Add(stringResources);
            }

            var newLangDict = new ResourceDictionary();
            try
            {
                newLangDict.Source = new Uri($"Strings\\{normalized}.xaml", UriKind.Relative);
            }
            catch
            {
                newLangDict.Source = new Uri("Strings\\en-US.xaml", UriKind.Relative);
            }

            stringResources.MergedDictionaries.Clear();
            stringResources.MergedDictionaries.Add(newLangDict);
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(normalized);
        }

        partial void OnEqualizerApoConfigPathChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = CheckWriteAccessAsync();
                if (!string.IsNullOrEmpty(PresetsFolderPath) && Presets.Any())
                {
                    _ = Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        var activePreset = await _presetService.SyncWithCurrentConfigAsync(value, Presets);
                        _isProgrammaticSelection = true;
                        SelectedPreset = activePreset;
                        _isProgrammaticSelection = false;
                    });
                }
            }
        }

        partial void OnSelectedPresetChanged(string? value)
        {
            OnPropertyChanged(nameof(IsPresetActive));
            if (!_isProgrammaticSelection && !string.IsNullOrEmpty(value))
            {
                _ = ApplyPresetAsync(value);
            }
        }

        public void Dispose()
        {
            _presetsFolderWatcher?.Dispose();
            lock (_timerLock)
            {
                _debounceTimer?.Dispose();
            }
        }
    }
}
