# 🎥 Unity Spectator Camera & 3D Virtual Studio Suite - v3.0.1

## 🌟 What's New in v3.0.1

### 🛡️ 1. Culture & Region Invariant Fix (Eliminates 30000+ / Multiplier Bug)
- **Global `CultureInfo.InvariantCulture`:** Guaranteed decimal point parsing across all Windows locales (English, Romanian, Russian, German, French, etc.).
- **Auto-Comma Replacement:** Any input typed with a comma `,` is automatically translated to a standard decimal point `.`.
- **Edit-Check Protection:** Text fields only parse when explicitly modified by the user (`if (newStr != str)`), eliminating frame-by-frame multiplication bugs.
- **Mouse Look Delta Clamping:** Added `Vector3.ClampMagnitude(delta, 80f)` to prevent sudden camera jumps when holding right-click across multiple monitors or during window focus transitions.

---

### 🔄 2. Reset Controls for Camera Transform
- **Master Reset Button (Orange):** `[ 🔄 Reset All Transform (Pos & Rot to 0) ]` instant 1-click restore.
- **Per-Axis Reset Buttons:** Added dedicated `[0]` and `[0°]` reset buttons next to X, Y, Z position and Pitch, Yaw, Roll rotation fields.
- **Section Reset Sub-Buttons:**
  - `[ 🔄 Reset Position (0, 0, 0) ]`
  - `[ 🔄 Reset Rotation (0°, 0°, 0°) ]`

---

### 🚀 3. Clean & Streamlined 1-Click Injector Kit
- Completely self-contained injection package without external dependencies.
- Compatible with all Unity Windows Mono builds (Match3MR / VR).

---

### 📦 Clean Package Contents:
- `Inject.bat` - Quick 1-click batch launcher
- `InjectSpectatorCamera.exe` - Standalone injector patcher
- `SpectatorPlugin.dll` - Camera runtime payload
- `README_INSTRUCTIONS.TXT` & `README.md` - Complete multilingual instructions (EN / RO / RU)
- `docs/` - Screenshot visual guides
