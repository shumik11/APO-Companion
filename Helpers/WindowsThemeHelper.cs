using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace APO.Helpers
{
    public static class WindowsThemeHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int attrSize);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        private const int DWMSBT_DISABLE = 1;
        private const int DWMSBT_MAIN = 2; // Mica
        private const int DWMSBT_TRANSIENT = 3; // Acrylic

        private const int WCA_ACCENT_POLICY = 19;

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

        public static void ApplyGlassEffect(Window window, bool isDark)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).EnsureHandle();
                var osVersion = Environment.OSVersion.Version;

                if (osVersion.Major >= 10 && osVersion.Build >= 22000)
                {
                    // Windows 11 (Build 22000+) - use Acrylic Backdrop
                    int backdropType = DWMSBT_TRANSIENT;
                    DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                }
                else if (osVersion.Major >= 10)
                {
                    // Windows 10 - use AccentPolicy
                    EnableWindows10Acrylic(hwnd, isDark);
                }
            }
            catch
            {
                // Fallback gracefully on error or older systems
            }
        }

        private static void EnableWindows10Acrylic(IntPtr hwnd, bool isDark)
        {
            var accent = new AccentPolicy
            {
                AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = isDark ? unchecked((int)0x99202020) : unchecked((int)0x99EEEEEE)
            };

            int accentStructSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
    }
}
