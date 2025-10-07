# APO Companion

<p align="center">
  <a href="README_ru.md">
    <img src="https://img.shields.io/badge/Читать-на%20Русском-blue?style=for-the-badge&logo=read-the-docs" alt="Русская версия"/>
  </a>
  <img src="https://img.shields.io/github/v/release/shumik11/APO-Companion?style=for-the-badge" alt="Latest Release"/>
</p>

A compact and stylish widget for the Windows desktop that allows you to quickly switch between configuration files (presets) for Equalizer APO. **No installation required.**

<div align="center">

| Active Mode | Inactive Mode | Tray Menu |
| :---: | :---: | :---: |
| ![Screenshot of the active widget](screenshots/active_widget.png) | ![Screenshot of the inactive widget](screenshots/unactive_widget.png) | ![Screenshot of the tray menu](screenshots/Tray.png) |
| _The widget becomes opaque when in focus._ | _The widget becomes semi-transparent when not in focus._ | _Quick access to all features from the system tray._ |

</div>

## 🚀 Features

-   **Light & Dark Themes:** Choose the look that best fits your desktop.
-   **Auto-Refresh Presets:** The list updates automatically when you add or remove preset files.
-   **Quick Switching:** Select equalizer presets directly from your desktop.
-   **Stylish Interface:** A modern design that looks great on any wallpaper.
-   **Multilingual Support:** The interface is available in English and Russian.
-   **Visibility Control:** Hide and show the widget through the system tray icon or the widget button.
-   **Easy Startup Management:** Add or remove the application from Windows startup with a single click.
-   **Smart Setup:** Automatically finds your Equalizer APO installation.

## 📥 Installation

No installation needed! The application is **portable**.

1.  Go to the [**Releases Page**](https://github.com/shumik11/APO-Companion/releases).
2.  Download the `.exe` file from the latest release.
3.  Run it.

On the first launch, the application will ask you to specify the path to Equalizer APO's `config.txt` file. Then, you can select the folder where your preset `.txt` files are stored.

## ⚙️ System Requirements

-   Windows 10 / 11 (may also work on Windows 7)
-   [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) installed

#### For Developers (Building from Source)
-   .NET 8 SDK & Desktop Runtime
-   Visual Studio 2022

## ❓ Frequently Asked Questions (FAQ)

**Q: I selected a folder, but nothing appeared in the presets list. Why?**

**A:** Make sure the folder you selected contains files with the `.txt` extension. The application only looks for these. Also, check that you have permission to read files in that directory. If there are presets in the folder but the list is empty, try restarting the application.

---

**Q: The application didn't find `config.txt` automatically. Where can I find it?**

**A:** Typically, `config.txt` is located in the Equalizer APO installation folder. The default path is:
`C:\Program Files\EqualizerAPO\config\config.txt`
If you installed Equalizer APO on a different drive or in another folder, look for it there. For example: `D:\MyPrograms\EqualizerAPO\config\config.txt`.

---

**Q: Can I edit presets directly from the widget?**

**A:** No. The widget is designed only for quickly switching between existing `.txt` files. To edit presets, use the standard Equalizer APO `Configuration Editor.exe`.

---

**Q: The widget disappeared/won't appear on the screen, but the tray icon is there. What should I do?**

**A:** The widget has likely moved off-screen. This can happen if you change your display resolution or monitor configuration.

To get it back, simply right-click the application icon in the system tray and select **"Reset Position"**. The widget will instantly reappear in the center of your main screen.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
