using Microsoft.Win32;
using System;

namespace APO.Services
{
    internal class AutorunService : IAutorunService
    {
        public bool IsInAutorun()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(Constants.AutorunRegistryKey, false))
            {
                return key?.GetValue(Constants.AppRegistryName) != null;
            }
        }

        public void SetAutorun(bool enable)
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(Constants.AutorunRegistryKey, true))
            {
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        throw new InvalidOperationException("Не удалось определить путь к приложению.");
                    }
                    key.SetValue(Constants.AppRegistryName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(Constants.AppRegistryName, false);
                }
            }
        }
    }
}
