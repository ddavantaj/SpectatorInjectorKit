# 🎥 Unity Spectator Camera & 3D Virtual Studio Suite - v3.1.2

## 🌟 What's New in v3.1.2

### 🙈 1. Clean Capture — Auto-Hide Panel During Screenshots & Recording
- The control panel now hides itself automatically the instant you press **F9** (screenshot) or start recording with **F10**, and restores itself automatically when the capture ends.
- No more control panel baked into your screenshots or MP4 videos.
- `F1` always takes priority — opening the panel manually during a recording is respected, and it won't be force-closed again.
- Built-in watchdogs guarantee the panel can never get stuck hidden, even if a capture is interrupted.
- The behavior is optional: toggle **"Hide this panel while capturing"** in the Video tab. Setting is saved and restored between sessions.

### 🗂️ 2. Redesigned Tabbed Control Panel
- Replaced the 7 stacked collapsible sections with a compact **tab bar** (Video · Tracking · Camera · Presets · 3D Scene · Backdrop · Console) — only one category renders at a time.
- Window height reduced from 980px to 640px by default.
- New persistent **status strip** always shows target, tracking mode, resolution/FPS, FOV, and a live REC timer.
- Capture buttons (Screenshot / Record) are pinned above the tabs so they're always reachable regardless of which tab is open.
- The window is now **resizable** from its bottom-right corner.
- Window position, size, active tab, and Clean Capture preference are all persisted in `SpectatorSettings.ini`.

### 🎯 3. Camera & Shape Position Sliders No Longer Clamp Your Values
- Previously, opening the Camera or 3D Scene tab while flying far from the origin would snap position sliders (and shape scale/position sliders) back within a fixed range.
- Slider ranges now grow automatically to always include the current value.

### 🕹️ 4. Mouse Look Sensitivity Exposed
- `lookSensitivity` was already used internally for mouse-look but had no UI control. It's now adjustable in the Tracking tab and persisted.

---

### 📦 Release Contents
- `SpectatorPlugin.dll` - Updated v3.1.2 camera runtime payload
- `SpectatorCameraController.cs` - Source code for v3.1.2
- `Inject.bat` and `InjectSpectatorCamera.exe` - Standalone injector
- `README.md` and `README_INSTRUCTIONS.TXT` - Updated documentation
