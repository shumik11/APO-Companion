using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace APO.Services
{
    internal class PresetService : IPresetService
    {
        public async Task<List<string>> LoadPresetsFromFolderAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return new List<string>();

            return await Task.Run(() => Directory.GetFiles(folderPath, Constants.PresetsFileFilter)
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f))
                .OfType<string>()
                .ToList());
        }

        public async Task ApplyPresetAsync(string? presetsFolderPath, string? equalizerApoConfigPath, string presetFileName)
        {
            if (string.IsNullOrEmpty(presetsFolderPath) || string.IsNullOrEmpty(equalizerApoConfigPath))
                return;

            string fullPresetPath = Path.Combine(presetsFolderPath, presetFileName);
            if (!File.Exists(fullPresetPath))
                return;

            string content = $"Include: {fullPresetPath}";
            await File.WriteAllTextAsync(equalizerApoConfigPath, content);
        }

        public async Task<string?> SyncWithCurrentConfigAsync(string equalizerApoConfigPath, List<string> presetFiles)
        {
            if (string.IsNullOrEmpty(equalizerApoConfigPath) || !File.Exists(equalizerApoConfigPath))
                return null;

            try
            {
                string currentConfig = await File.ReadAllTextAsync(equalizerApoConfigPath);
                return presetFiles.FirstOrDefault(presetFile => currentConfig.Contains(presetFile));
            }
            catch (IOException)
            {
                // Ignore
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore
            }

            return null;
        }
    }
}
