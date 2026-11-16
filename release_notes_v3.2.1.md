# 🎥 Unity Spectator Camera & 3D Virtual Studio Suite - v3.2.1

## 🌟 What's New in v3.2.1

### 🧰 Graphical Installer
- Double-click **`Instaleaza.cmd`** to open a simple installer window — no command line needed.
- Pick the game folder (browse or drag & drop); the window shows whether it is a valid Unity game, whether Spectator is installed and whether `ffmpeg` is present.
- Buttons: **Install / Update**, **Uninstall** (restores the original game files), **Install ffmpeg** (copied from the kit or downloaded once, ~90 MB) for F10 video recording.
- Refuses to modify files while the game is running; remembers the last folder; can launch the game and open the web panel.
- `Inject.bat` / `InjectSpectatorCamera.exe` still work for command-line use (`--uninstall` restores the original files).

### 📦 Release Contents
- `Instaleaza.cmd` + `Installer.ps1` - Graphical installer
- `SpectatorPlugin.dll`, injector and sources - unchanged from v3.2.0
- `README.md`, `README_INSTRUCTIONS.TXT`, `CITESTE-*.txt` - Documentation
