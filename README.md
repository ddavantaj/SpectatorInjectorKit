# 🎥 Unity Match-3 MR Spectator Camera & 3D Virtual Studio Suite (v2.8.0)

[![Release](https://img.shields.io/badge/Release-v2.8.0-brightgreen.svg)](https://github.com/ddavantaj/SpectatorInjectorKit/releases/tag/v2.8.0)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Unity%20MR%20%2F%20VR-blue.svg)]()
[![License](https://img.shields.io/badge/License-MIT-purple.svg)]()

A high-performance, standalone Spectator Camera, 3D Virtual Studio Set, and Screen Capture Suite designed for Unity Mixed Reality (MR / OpenXR) Windows builds.

Works as a **1-click standalone injector on any compiled Windows game build** (without needing Unity Editor or project rebuilding) or as a native Unity C# script component.

---

## ⚡ Download & Quick Start

### 📦 1-Click Standalone Injector (No Unity Editor Required)
1. Download the latest pre-packaged release: **[SpectatorInjectorKit_v2.8.0.zip](https://github.com/ddavantaj/SpectatorInjectorKit/releases/download/v2.8.0/SpectatorInjectorKit_v2.8.0.zip)**.
2. Extract the ZIP archive anywhere on your PC.
3. Double-click **`Inject.bat`** (or `InjectSpectatorCamera.exe`).
4. Select your target game folder when prompted (the folder containing `Match3MR.exe` or your game `.exe`).
5. Launch the game executable. The Spectator Camera and Control Panel will appear automatically on your screen!

### 🛠️ Unity Project Integration
* Include `SpectatorCameraController.cs` in your `Assets/Scripts/` folder.
* It auto-initializes upon scene load via `[RuntimeInitializeOnLoadMethod]`.

---

## ⌨️ Hotkeys & Control Reference

| Key / Shortcut | Action | Description |
| :--- | :--- | :--- |
| **`F1`** | **Toggle UI** | Show / Hide the Spectator Suite on-screen Control Panel. |
| **`F9`** | **Take Screenshot** | Capture ultra-crisp PNG screenshot (8x MSAA & Alpha transparency support). |
| **`F10`** | **Toggle Recording** | Start / Stop 60 FPS MP4 video recording with **1:1 Real-Time Speed Sync**. |
| **`W / A / S / D`** | **Move Camera** | Fly Forward, Left, Backward, Right in 3D World Space. |
| **`E / Space`** | **Fly Up** | Elevate camera upwards in World Space. |
| **`Q / Left Ctrl`** | **Fly Down** | Lower camera downwards in World Space. |
| **`Left Shift`** | **Speed Boost** | Hold to move 3.5x faster. |
| **`Right-Click Drag`** | **Mouse Look** | Orbit, pitch, and yaw camera view. |

---

## 🌟 Key Features & Capabilities

### 1. 🧱 Physical 3D In-Game Shapes & Virtual Studio (True 3D Parallax)
The background is an **actual 3D physical object anchored in the game scene**, providing authentic 3D perspective and depth parallax when the spectator camera moves around the game board.

* **3D Virtual Studio Modes**:
  * 🖼️ **`Curved Studio Backdrop (Cyclorama)`**:
    * **`Bend Sides (Wrap X)`**: Real-time horizontal mesh curvature from `0°` (flat) to `110°` (studio cyc) to `240°` (full wrap-around).
    * **`Bend Top/Bottom (Wrap Y)`**: Real-time vertical mesh curvature from `0°` (flat) to `120°` (dome / bowl wrap).
  * 🌐 **`360° Sphere Dome`**: Inverted 360° sphere mesh for panoramic HDRI environments.
  * 🔲 **`Flat Wall`**: Classic flat 3D studio wall / backdrop poster.
* **Native Pure C# Radiance HDR (.hdr RGBE) Decoder**: Binary decoder with Reinhard tonemapping (no external libraries needed). Also supports standard PNG, JPG, and BMP formats.
* **Manual Numeric Scale Inputs**: Direct text boxes to type exact dimensions for `Width (Scale X)`, `Height (Scale Y)`, and `Depth / Radius (Scale Z)`.
* **`[ 🎯 Place Behind View ]`**: Instantly snaps the 3D shape 8m in front of current camera view and anchors it permanently in 3D world space.
* **Brightness / Exposure Slider**: Adjust ambient illumination from `0.05x` to `5.0x`.

---

### 2. 📸 Ultra-Crisp Screenshots & 🎥 1:1 Real-Time Synced Video Recording
* **Direct Windows Backbuffer Capture**: Reads directly from the resolved Windows monitor frame (`WaitForEndOfFrame`), capturing full anti-aliasing, post-processing, shaders, and game UI.
* **1:1 Real-Time Video Speed Synchronization**:
  * Calculates exact elapsed real-time (`Time.realtimeSinceStartup`) to feed frame quotas to FFmpeg.
  * Guarantees 1 second in game = 1 second in video (eliminating fast-forward / time-skew effects on 72/90/120 Hz VR headsets).
* **Hardware-Accelerated Encoding**: Non-blocking asynchronous worker thread streaming frames to FFmpeg with `NVENC H.264` (GPU) and automatic fallback to `libx264` (CPU).
* **Alpha PNG Cutouts**: Enable *Disable Skybox* and set *Alpha = 0* to produce transparent PNG screenshots with 8x MSAA edge smoothing.

---

### 3. 💾 Camera-Only Presets (P1 – P4)
* **`Save Cam P1 - P4` / `Load Cam P1 - P4`**:
  * Saves and restores **only the camera properties** (Position, Rotation, FOV, Speed, Lock).
  * 🔒 **Skybox & 3D Backdrop Preservation**: Switching camera presets will **never** alter, reset, or override your loaded 3D environment or cyclorama settings.

---

### 4. 🔁 Full Automatic Persistence (`SpectatorSettings.ini`)
* All settings automatically save every 5 seconds, upon closing the application (`OnApplicationQuit`), or by clicking `[ 💾 Save Now ]`.
* **When reopening the game, everything is automatically restored**:
  1. Exact Camera Position, Rotation, FOV, and Speed.
  2. Active 3D shape file path and loaded texture.
  3. 3D Shape World Position, Rotation, Scale X/Y/Z, and Curvature Angles (`Bend Sides` / `Bend Top/Bottom`).
  4. Shape Brightness and Solid Color / Transparency configurations.
  5. All 4 saved Camera Presets.
  6. Proxima Terminal input values.

---

### 5. 💻 Proxima Inspector & In-Game Cheats
* **Quick Action Cheats**:
  * `Set Level` / `Load Level` / `Load Menu`
  * `Set Coins (set_coins)`
  * `Set Lives (set_lives)`
  * `Set Stars (set_stars)`
  * `Win Level (win_level)` / `Lose Level (lose_level)`
  * `Recenter Area` / `Reset Tutorials` / `Clear Save`
* **Custom Command Console**: Execute any internal game or Proxima method dynamically via reflection directly from the in-game UI.

---

## 📁 File Structure & Storage

```text
Captures/
├── Screenshots/          # F9 PNG screenshots
├── Videos/               # F10 MP4 video recordings
├── CustomSkyboxes/       # User HDR/JPG/PNG background files
└── SpectatorSettings.ini # Persistent settings storage
```

---

## 📖 Multilingual Documentation

For instructions in Romanian and Russian, please see **[`README_INSTRUCTIONS.TXT`](./README_INSTRUCTIONS.TXT)**.

---

## 📄 License
This project is licensed under the MIT License.
