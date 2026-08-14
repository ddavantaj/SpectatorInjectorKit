# 🚀 Unity Spectator Camera & Capture Suite v3.0.0

A major release introducing **high-performance zero-lag 60 FPS video recording**, **cinematic Steadicam video stabilization**, and a **clean, categorized accordion UI system**.

---

## 🌟 What's New in v3.0.0

### 1. 🗂️ Categorized Accordion Control Panel
* Reorganized the entire Spectator UI into 7 clean, collapsible foldout boxes (`[ ▲ Hide / ▼ Open ]`) with real-time status badges:
  1. **🎥 1. Video Setup & Stabilization** `[ 1080p @ 60 FPS | 🎬 Steadicam ON ]`
  2. **👤 2. VR Tracking & Free-Flight** `[ Following VR / Locked ]`
  3. **🎥 3. Camera Position, Rotation & FOV** `[ FOV: 60° ]`
  4. **💾 4. Camera Presets** `[ 4 Slots ]`
  5. **🧱 5. 3D Scene Objects & Cyclorama** `[ Active / None ]`
  6. **🎨 6. Skybox & Background Fill** `[ Solid / Transparent ]`
  7. **💻 7. Proxima In-Game Inspector & Cheats** `[ Terminal & Cheats ]`

### 2. 🎬 High-Performance Zero-Lag 60 FPS Video Recording (MP4)
* **Zero-GC Buffer Recycling Pool**: Pre-allocated frame buffer pool recycling memory with **0 MB/s GC allocation**, eliminating video capture stuttering.
* **NVENC Ultra-Low Latency (`-tune ull -preset p1`)**: Hardware-accelerated GPU encoding on a dedicated low-priority worker thread.
* **Multi-Resolution Support**: Choose between `1080p (Full HD)`, `720p (Ultra Fast)`, or `Native Screen`.
* **1:1 Real-Time Speed Sync**: Captures exact frame intervals based on real-time elapsed timestamps, preventing VR fast-forward effects.

### 3. 🎥 Cinematic Video Stabilization (Steadicam / Gimbal Damping)
* Real-time positional and spherical rotational damping (`Vector3.Lerp` / `Quaternion.Slerp`).
* Built-in one-click damping presets:
  * `Subtle (12.0)` — Responsive handheld look.
  * `Cinematic (6.0)` — Smooth Steadicam camera motion.
  * `Heavy/Drone (2.5)` — Heavy gimbal / drone camera flight.
* Interactive slider for fine-tuning damping smoothness (`1.5` to `20.0`).

### 4. 👤 1:1 VR Headset Follow & Flight Modes
* **`Follow Main Camera (VR Headset / 1st Person)`**: Real-time tracking of the player's VR Headset for immersive gameplay footage.
* **`Lock World Position (Static PC Camera)`**: Fixed world camera for external spectator views and promotional framing.

### 5. 💾 Full Automatic Persistence (`SpectatorSettings.ini`)
* Automatically saves and restores camera coordinates, presets, 3D studio cyclorama properties, video resolution, and stabilization parameters across game sessions.

---

## 📦 Download & Quick Start

1. Download **`SpectatorInjectorKit_v3.0.0.zip`** below.
2. Extract the archive anywhere on your PC.
3. Run **`Inject.bat`** (or `InjectSpectatorCamera.exe`).
4. Select your target game build folder.
5. Launch the game and enjoy the full Spectator Camera & Capture Suite!
