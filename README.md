# NewYearTree

A transparent Windows desktop Christmas tree with animated lights, swaying ornaments, snowfall and a countdown to New Year.

[Download the latest release](https://github.com/Forward808/new-year-tree/releases/latest) · [User guide](docs/USER_GUIDE.md)

<img src="docs/screenshot.png" alt="Christmas tree widget with a countdown and presents on a snowy base" width="320">

## Quick start

1. Open **Releases** and download **NewYearTree.exe** from the release assets.
2. Save it to any folder and double-click it. No installer is needed.
3. Double-click the tree to open settings.
4. Find **Язык / Language** at the top and select **English** if needed.

Only the EXE is needed. All images are embedded and the app works offline.

Drag with the left mouse button to move the tree. Right-click to open its menu. Choose **Exit** to close it.

## Features

- Russian and English interface with instant language switching.
- Countdown in days, hours and minutes using the Windows local clock.
- Five light modes, three color palettes, brightness and speed controls.
- Twelve ornaments with optional gentle swaying.
- Three snowfall intensities, snow-covered branches and gifts on a snowy base.
- Adjustable size, dragging, position lock and always-on-top mode.
- Settings saved automatically in `%LOCALAPPDATA%\NewYearTree\settings.xml`.
- All images embedded in a single executable; no internet connection required.

## Run

Download or copy `dist/NewYearTree.exe` and run it on Windows with .NET Framework 4.x installed. No other project files are needed to run the application.

Drag with the left mouse button. Right-click to open the menu. Double-click to open settings. The notification area icon also provides access to settings, visibility and exit.

The countdown format is `days:hours:minutes`. Days have no leading zero. The target changes to the following January 1 when the year changes.

## Settings

- **Size:** 55% to 150%.
- **Sway ornaments:** enable or disable gentle movement and adjust its amount.
- **Light mode:** Twinkle, Alternating lights, Chase, Slow glow or Steady lights.
- **Light colors:** Multicolor, Warm gold, or Ice blue and white.
- **Brightness and speed:** adjust the lights to your preference.
- **Falling snow:** turn snowfall on or off and choose Light, Medium or Heavy intensity.
- **Ornaments and countdown:** show or hide them independently.
- **Always on top:** keep the tree above other windows.
- **Lock tree position:** prevent accidental dragging.

Snow on the branches and the snowy ground remain visible when falling snow is disabled.

## Hide, recover and update

Use the notification area icon to show or hide the tree. If the icon is hidden, open the overflow menu next to the Windows clock. Choose **Bring tree back on screen** if you cannot find the widget.

To update, exit the running widget before replacing its EXE. Your preferences remain saved in your Windows user profile.

To reset settings, close the widget and delete `%LOCALAPPDATA%\NewYearTree\settings.xml`.

## Build

Run from PowerShell on Windows:

```powershell
.\build.ps1
```

The script uses the .NET Framework C# compiler included with the framework. Output: `dist/NewYearTree.exe`. No NuGet packages are required.

## Project files

- `src/NewYearTree.cs` — application source.
- `assets/` — PNG assets embedded during compilation.
- `build.ps1` — build script.
- `dist/NewYearTree.exe` — ready-to-run executable built from this source.

Visual assets were generated with OpenAI image generation and assembled into independently animated layers by the application.

## По-русски

Прозрачная ёлочка для рабочего стола Windows: гирлянды, покачивающиеся игрушки, снег и обратный отсчёт до Нового года.

Запуск: `dist/NewYearTree.exe`. Для переноса достаточно одного EXE. Нужен .NET Framework 4.x.

Правая кнопка мыши открывает меню, двойной щелчок — настройки. Перетаскивание — левой кнопкой. Выбор «Язык / Language» находится в начале настроек.

Сборка из исходников: `.\build.ps1`. Настройки сохраняются в профиле Windows.
