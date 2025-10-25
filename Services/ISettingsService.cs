using System.Collections.Specialized;

namespace APO.Services
{
    public interface ISettingsService
    {
        void SaveWindowPosition(double top, double left);
        (double Top, double Left) LoadWindowPosition();
        void SavePresetsFolderPath(string path);
        string LoadPresetsFolderPath();
        void SaveLanguage(string langCode);
        string LoadLanguage();
        void SaveTheme(string themeName);
        string LoadTheme();
        string LoadEqualizerApoConfigPath();
        void SaveEqualizerApoConfigPath(string path);
        StringCollection LoadRecentPresets();
        void UpdateRecentPresets(string presetFileName);
    }
}
