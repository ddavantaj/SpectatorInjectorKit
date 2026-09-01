# 🎥 Unity Spectator Camera & 3D Virtual Studio Suite - v3.1.0

## 🌟 What's New in v3.1.0

### 🔊 1. Synchronized In-Game Audio Recording (MP4 with Audio)
- **Direct Unity Master Audio Capture:** Automatically hooks into the scene's active `AudioListener` (`SpectatorAudioListenerHook`) to intercept mixed game audio via `OnAudioFilterRead`.
- **Crystal-Clear Game Sound:** Captures 16-bit stereo PCM digital audio directly from Unity's audio engine (music, sound effects, UI clicks, VR spatial audio) with zero microphone noise or room interference.
- **Zero-GC Audio Pool:** Employs pre-allocated audio buffer recycling (`ConcurrentQueue<byte[]>`), guaranteeing **0 MB/s GC allocation** to keep frame rates silky smooth in VR.
- **Ultra-Fast Stream Copy Muxing:**
  - Video is recorded in real time with hardware NVENC/x264.
  - Upon stopping recording (`F10`), FFmpeg performs a lossless stream copy (`-c:v copy -c:a aac -b:a 192k -ac 2`), interleaving audio and video in under 0.3 seconds without re-encoding video frames.
- **100% Fail-Safe Fallback:** If audio is disabled, empty, or absent, the capture engine gracefully saves the video without error or data loss.

---

### 🎛️ 2. Audio UI Controls & Persistence
- **Live Status Badges:** Section 1 header displays real-time audio state: `[ 1080p @ 60 FPS | 🎬 Steadicam ON | 🔊 Audio ON ]`.
- **Interactive Audio Toggle:** Turn audio recording on or off at any time with a single click: `[x] 🔊 Record In-Game Audio (Direct Master Sound)`.
- **Persistent Settings:** `recordAudio` state is automatically saved and restored via `Captures/SpectatorSettings.ini`.

---

### 📦 Clean Package Contents:
- `Inject.bat` - 1-click batch launcher
- `InjectSpectatorCamera.exe` - Standalone injector patcher
- `SpectatorPlugin.dll` - Updated v3.1.0 camera runtime payload
- `README_INSTRUCTIONS.TXT` & `README.md` - Complete multilingual instructions (EN / RO / RU)
- `docs/` - Screenshot visual guides
