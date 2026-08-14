# 🌐 Universal Unity VR Spectator Camera & 3D Virtual Studio Suite (v1.0.0-Universal)

The **Universal Edition** of the Spectator Camera & Capture Suite, designed to work with **ANY Unity VR / Mixed Reality game build** on Windows (OpenXR, Meta/Oculus XR SDK, SteamVR, XR Interaction Toolkit), even if the original game build **never had a spectator camera**.

---

## 🌟 Universal VR Highlights

### 1. ⚡ Dynamic Spectator Camera Auto-Creation
* If a game build has no spectator camera in the scene, the plugin automatically creates a dedicated desktop spectator camera at runtime (`stereoTargetEye = StereoTargetEyeMask.None`, `depth = 999`), rendering exclusively to the PC display while keeping the VR headset view completely intact.

### 2. 👤 3 Intelligent VR Tracking Modes
* 🎯 **`1st Person (Headset 1:1)`**: Tracks the player's VR headset (`CenterEyeAnchor` / `Camera.main`) in real-time.
* 🎥 **`3rd Person (Over-The-Shoulder / Drone Follow)`**: Smooth cinematic follow behind/above the player with adjustable distance (`0.5m` - `6.0m`) and height (`-0.5m` - `2.5m`).
* 🛸 **`Free Flight (3D World Space)`**: Fly anywhere freely in the game world with `WASD` + `Right-Click Mouse Look`.

### 3. 🎬 High-Performance Zero-Lag 60 FPS Video Recording (MP4)
* Zero-GC buffer pool with 0 MB/s allocation and hardware NVENC Ultra-Low Latency encoding.
* 1080p, 720p, and Native resolution modes with 1:1 real-time speed synchronization.

### 4. 🎥 Cinematic Steadicam Video Stabilization
* Dynamic positional and spherical rotational damping (`Vector3.Lerp` / `Quaternion.Slerp`).
* Built-in presets: `Subtle (12.0)`, `Cinematic (6.0)`, `Heavy/Drone (2.5)` + interactive damping slider.

### 5. 🧱 Physical 3D In-Game Shapes & Virtual Studio (True 3D Parallax)
* Curved Studio Backdrop (Cyclorama), 360° HDRI Environment Sphere, Flat Wall, Pure C# Radiance HDR loader.

### 6. 🎨 Skybox & Background Transparency
* Solid color fills and transparent PNG cutouts (Alpha = 0) with 8x MSAA edge smoothing.

### 7. 💾 Universal 1-Click Injector (`Inject.bat` / `InjectSpectatorCamera.exe`)
* Automatically hooks into `Assembly-CSharp.dll` of any Unity game build on Windows.

---

## 📦 Download & Quick Start

1. Download **`UniversalSpectatorKit_v1.0.0.zip`** below.
2. Extract the archive anywhere on your PC.
3. Run **`Inject.bat`** (or `InjectSpectatorCamera.exe`).
4. Drag and drop any Unity game build folder or `.exe` into the console, or pick it from the dialog.
5. Launch the game executable and enjoy the Universal Spectator Suite!
