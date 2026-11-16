# 🎥 Unity Spectator Camera & 3D Virtual Studio Suite - v3.2.0

## 🌟 What's New in v3.2.0

### 🌐 1. Spectator Studio Web Panel
- Control the camera, captures, 3D scene and backdrop from your browser at `http://127.0.0.1:47830/` (or run `Panou Web.cmd`).
- The server listens on `127.0.0.1` only — it is never exposed to the local network.
- The page is embedded in `SpectatorPlugin.dll`; nothing extra to install.

### ⌘ 2. Proxima Command Console
- The in-game **Console** tab (F1) and the web panel list the game's Proxima commands, grouped by category, with search, parameters and a result log.
- Sources are in `ProximaCore/` and `ProximaPanel.cs`.

### 🎬 3. Video Capture
- `ffmpeg.exe` is looked up next to the game executable. It is **not** included in this repository or archive (too large for GitHub) — place it next to the game `.exe` to enable F10 recording.

### 🖼️ 4. Updated Screenshots & Docs
- New in-game panel screenshot and a web panel screenshot in the README.
- `BuildPlugin.bat` and `SpectatorPlugin.csproj` rebuild the adapted plugin.

---

### 📦 Release Contents
- `SpectatorPlugin.dll` - Updated runtime payload (includes web panel)
- `SpectatorCameraController.cs`, `SpectatorWeb.cs`, `ProximaPanel.cs`, `ProximaCore/` - Source code
- `Inject.bat` and `InjectSpectatorCamera.exe` - Standalone injector
- `README.md`, `README_INSTRUCTIONS.TXT`, `CITESTE-*.txt` - Documentation
