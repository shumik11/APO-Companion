using System.Collections.Generic;
using System.Threading.Tasks;

namespace APO.Services
{
    public interface IPresetService
    {
        Task<List<string>> LoadPresetsFromFolderAsync(string folderPath);
        Task ApplyPresetAsync(string? presetsFolderPath, string? equalizerApoConfigPath, string presetFileName);
        Task<string?> SyncWithCurrentConfigAsync(string equalizerApoConfigPath, List<string> presetFiles);
        Task<bool> HasWriteAccessAsync(string filePath);
    }
}
