# 🎥 Unity Spectator Camera & 3D Virtual Studio Suite - v3.1.1

## 🌟 What's New in v3.1.1

### 🌐 1. Fixed 360° HDRI Sphere Visibility
- Corrected the sphere mesh winding so its textured faces render from inside the dome.
- HDRI panoramas now display correctly around the spectator camera.

### 📌 2. Flat Wall Follows the Spectator Camera
- Added **Follow Spectator Camera (Always in View)** for the Flat Wall background.
- When enabled, the background keeps its position and full rotation aligned to the active spectator camera.
- The setting is saved in `SpectatorSettings.ini` and restored on the next launch.

### 🎯 3. Improved Place Behind View
- **Place Behind View** now uses the actual spectator camera transform.
- It copies the complete camera orientation (pitch, yaw, and roll) and positions the shape in the camera view direction.

---

### 📦 Release Contents
- `SpectatorPlugin.dll` - Updated v3.1.1 camera runtime payload
- `SpectatorCameraController.cs` - Source code for v3.1.1
- `Inject.bat` and `InjectSpectatorCamera.exe` - Standalone injector
- `README.md` and `README_INSTRUCTIONS.TXT` - Updated documentation
