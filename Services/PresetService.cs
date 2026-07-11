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
                throw new System.ArgumentException("Путь к пресетам или конфигурации не задан.");

            string fullPresetPath = Path.Combine(presetsFolderPath, presetFileName);
            if (!File.Exists(fullPresetPath))
                throw new FileNotFoundException($"Файл пресета не найден: {fullPresetPath}");

            string content = $"Include: {fullPresetPath}";
            
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await File.WriteAllTextAsync(equalizerApoConfigPath, content);
                    return;
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1) throw;
                    await Task.Delay(50);
                }
                catch (System.UnauthorizedAccessException)
                {
                    throw new System.UnauthorizedAccessException("Отказано в доступе к файлу конфигурации. Попробуйте перезапустить программу от имени администратора.");
                }
            }
        }

        public async Task<string?> SyncWithCurrentConfigAsync(string equalizerApoConfigPath, List<string> presetFiles)
        {
            if (string.IsNullOrEmpty(equalizerApoConfigPath) || !File.Exists(equalizerApoConfigPath))
                return null;

            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    string currentConfig = await File.ReadAllTextAsync(equalizerApoConfigPath);
                    return presetFiles.FirstOrDefault(presetFile => 
                        currentConfig.Contains($"\\{presetFile}", System.StringComparison.OrdinalIgnoreCase) || 
                        currentConfig.Contains($"/{presetFile}", System.StringComparison.OrdinalIgnoreCase) || 
                        currentConfig.Contains($" {presetFile}", System.StringComparison.OrdinalIgnoreCase));
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    await Task.Delay(50);
                }
                catch (System.UnauthorizedAccessException)
                {
                    break;
                }
            }

            return null;
        }

        public async Task<bool> HasWriteAccessAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            return await Task.Run(() =>
            {
                try
                {
                    using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
