using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace APO
{
    public static class DesktopWidgetHelper
    {
        // Константы для работы со стилями окна
        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TRANSPARENT = 0x20;

        // Импорт функций из WinAPI
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        /// <summary>
        /// "Приклеивает" указанное окно к рабочему столу.
        /// </summary>
        public static void AttachToDesktop(Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            IntPtr desktopHandle = FindDesktopHandle();
            SetParent(windowHandle, desktopHandle);
        }

        /// <summary>
        /// Делает окно "прозрачным" для кликов мыши.
        /// </summary>
        public static void EnableClickThrough(Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            uint extendedStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
            SetWindowLong(windowHandle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// Возвращает окну нормальную интерактивность.
        /// </summary>
        public static void DisableClickThrough(Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            uint extendedStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
            SetWindowLong(windowHandle, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// Находит "ручку" (handle) рабочего стола для встраивания.
        /// </summary>
        private static IntPtr FindDesktopHandle()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr result = IntPtr.Zero;

            // Обходной путь для современных версий Windows
            IntPtr workerW = IntPtr.Zero;
            do
            {
                workerW = FindWindowEx(progman, workerW, "WorkerW", null);
                result = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
            } while (result == IntPtr.Zero && workerW != IntPtr.Zero);

            return result != IntPtr.Zero ? result : progman;
        }
    }
}