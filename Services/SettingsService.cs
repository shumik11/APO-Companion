using System.Collections.Specialized;
using System.Windows;

namespace APO.Services
{
    internal class SettingsService : ISettingsService
    {
        public void SaveWindowPosition(double top, double left)
        {
            Properties.Settings.Default.WindowTop = top;
            Properties.Settings.Default.WindowLeft = left;
            Properties.Settings.Default.Save();
        }

        public (double Top, double Left) LoadWindowPosition()
        {
            double savedTop = Properties.Settings.Default.WindowTop;
            double savedLeft = Properties.Settings.Default.WindowLeft;
            return (savedTop, savedLeft);
        }

        public void SavePresetsFolderPath(string path)
        {
            Properties.Settings.Default.PresetsFolderPath = path;
            Properties.Settings.Default.Save();
        }

        public string LoadPresetsFolderPath()
        {
            return Properties.Settings.Default.PresetsFolderPath;
        }

        public void SaveLanguage(string langCode)
        {
            Properties.Settings.Default.Language = langCode;
            Properties.Settings.Default.Save();
        }

        public string LoadLanguage()
        {
            return Properties.Settings.Default.Language;
        }

        public void SaveTheme(string themeName)
        {
            Properties.Settings.Default.Theme = themeName;
            Properties.Settings.Default.Save();
        }

        public string LoadTheme()
        {
            return Properties.Settings.Default.Theme;
        }

        public string LoadEqualizerApoConfigPath()
        {
            return Properties.Settings.Default.EqualizerApoConfigPath;
        }

        public void SaveEqualizerApoConfigPath(string path)
        {
            Properties.Settings.Default.EqualizerApoConfigPath = path;
            Properties.Settings.Default.Save();
        }

        public StringCollection LoadRecentPresets()
        {
            return Properties.Settings.Default.RecentPresets;
        }

        public void UpdateRecentPresets(string presetFileName)
        {
            var recents = Properties.Settings.Default.RecentPresets;
            if (recents == null)
            {
                recents = new StringCollection();
            }

            if (recents.Contains(presetFileName))
            {
                recents.Remove(presetFileName);
            }

            recents.Insert(0, presetFileName);

            while (recents.Count > Constants.MaxRecentPresets)
            {
                recents.RemoveAt(recents.Count - 1);
            }

            Properties.Settings.Default.RecentPresets = recents;
            Properties.Settings.Default.Save();
        }
    }
}
