using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SpectatorTools
{
    public class SpectatorCameraController : MonoBehaviour
    {
        public const string VERSION = "v1.0.0-Universal";
        public static SpectatorCameraController Instance { get; private set; }

        private Transform targetTransform;
        private Camera targetCamera;
        private string targetName = "Universal_SpectatorCamera";

        private bool showUI = true;
        private Rect windowRect = new Rect(30, 30, 490, 980);
        private bool isMinimized = false;
        private Vector2 mainScrollPos;
        private Vector2 logScrollPos;

        // Camera parameters in World Space
        private Vector3 pos;
        private Vector3 rot;
        private float fov = 60f;
        private float moveSpeed = 2f;
        private float lookSensitivity = 3f;

        // VR Tracking & Flight Modes (Universal for any VR game)
        public enum FollowMode
        {
            FirstPerson_1to1,      // Follow player VR headset 1:1 in real time
            ThirdPerson_Shoulder,  // Smooth cinematic over-the-shoulder / drone follow
            FreeFlight_World       // Fixed static 3D world coordinates (WASD + Mouse)
        }

        private FollowMode currentFollowMode = FollowMode.FirstPerson_1to1;
        private float thirdPersonDistance = 1.8f;
        private float thirdPersonHeight = 0.35f;

        private bool isPosInitialized = false;
        private Transform originalParent;
        private bool isUnparented = false;

        // Background & Skybox
        private bool disableSkybox = false;
        private Color bgColor = Color.black;
        private CameraClearFlags originalClearFlags = CameraClearFlags.Skybox;

        // ==========================================
        // 🧱 3D PHYSICAL SCENE OBJECTS (TRUE 3D PARALLAX)
        // ==========================================
        public enum Shape3DType
        {
            CurvedBackdrop3D, // 3D Curved Studio Backdrop / Cyclorama (Bend X & Y)
            Sphere360HDRI,    // 3D Environment Sphere surrounding world
            FlatWall3D        // 3D Flat Wall / Poster
        }

        private Shape3DType currentShape = Shape3DType.CurvedBackdrop3D;
        private GameObject shapeObject = null;
        private MeshRenderer shapeRenderer = null;
        private Material shapeMaterial = null;
        private Texture2D loadedShapeTexture = null;
        private bool show3DShape = true;

        // Fixed 3D World Transform of the In-Game Shape
        private Vector3 shapeWorldPos = new Vector3(0f, 1.2f, 8f);
        private Vector3 shapeWorldRot = new Vector3(0f, 0f, 0f);
        private Vector3 shapeScale = new Vector3(25f, 15f, 25f);
        private bool isShapePlacedInWorld = false;

        // Curvature / Bend Sliders
        private float bendSidesAngle = 110f;    // Horizontal curvature (0° Flat to 240° Studio Cyc)
        private float bendTopBottomAngle = 35f; // Vertical curvature (0° Flat to 120° Bowl)
        private float shapeBrightness = 1.0f;
        private string loadedShapeFilePath = "";
        private string loadedShapeFileName = "";

        // ==========================================
        // 💾 CAMERA-ONLY PRESETS STRUCTURE
        // ==========================================
        [System.Serializable]
        public struct CamPreset
        {
            public float posX, posY, posZ;
            public float rotX, rotY, rotZ;
            public float fov;
            public float moveSpeed;
            public bool lockWorldPos;
            public bool isSaved;
        }

        private CamPreset[] presets = new CamPreset[4];

        // Mouse look state
        private bool isRightClickDragging = false;
        private Vector3 lastMousePos;

        // Proxima Inspector Terminal & Inputs
        private string terminalInput = "";
        private string terminalLog = $"Spectator Camera & Capture Suite {VERSION} Active.\nEnter a command or click buttons below.";

        private string inputLevelStr = "1";
        private string inputCoinsStr = "9999";
        private string inputLivesStr = "99";
        private string inputStarsStr = "999";

        // Screen Capture & High-Performance Zero-Lag Recording State
        public enum VideoResMode { FullHD_1080p, HD_720p, NativeDisplay }
        public enum VideoQualityMode { UltraFast_ZeroLag, Balanced, HighQuality }

        private bool isRecording = false;
        private float recordingRealStartTime = 0f;
        private int recordedFramesCount = 0;
        private Process ffmpegProcess = null;
        private Stream ffmpegStdin = null;
        private Thread encodingThread = null;
        private BlockingCollection<byte[]> frameQueue = null;
        private ConcurrentQueue<byte[]> bufferPool = new ConcurrentQueue<byte[]>();
        private bool stopEncodingThread = false;
        private int recordWidth = 1920;
        private int recordHeight = 1080;
        private VideoResMode videoResMode = VideoResMode.FullHD_1080p;
        private int targetRecordFPS = 60;
        private VideoQualityMode videoQuality = VideoQualityMode.UltraFast_ZeroLag;
        private string currentVideoPath = "";
        private string captureBannerNotice = "";
        private float captureBannerTimer = 0f;
        private bool takeScreenshotOnNextFrame = false;

        // UI Categorized Foldout Sections
        private bool showSectionVideo = false;
        private bool showSectionTracking = true;
        private bool showSectionCamera = true;
        private bool showSectionPresets = false;
        private bool showSection3DShape = false;
        private bool showSectionSkybox = false;
        private bool showSectionProxima = false;

        // Save & Search timers
        private float lastSearchTime = -10f;
        private float lastAutoSaveTime = 0f;
        private bool isSettingsLoaded = false;
        private Transform cachedMainVRCamera = null;

        // Video Stabilization & Steadicam Damping
        public enum StabilizationPreset { Off, Low, Medium, Heavy }
        private bool enableStabilization = true;
        private float stabilizationSmoothness = 6.0f; // 6.0 = Medium (Cinematic Steadicam)
        private Vector3 smoothCurrentPos = Vector3.zero;
        private Quaternion smoothCurrentRot = Quaternion.identity;
        private bool isStabilizationInit = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoInitialize()
        {
            try
            {
                if (Instance != null) return;
                GameObject go = new GameObject("[SpectatorCameraController]");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<SpectatorCameraController>();
                Debug.Log($"[SpectatorCameraController] Injected and initialized successfully ({VERSION})!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SpectatorCameraController] AutoInitialize deferred: {ex.Message}");
            }
        }

        private void Awake()
        {
            EnsureCaptureDirectories();
            LoadAllPersistentSettings();
        }

        private void Start()
        {
            FindTargetCamera();
            StartCoroutine(CaptureRecordCoroutine());
        }

        private void Update()
        {
            // Toggle UI on/off with F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showUI = !showUI;
            }

            // Shortcut: F9 = Take Screenshot (Alpha PNG)
            if (Input.GetKeyDown(KeyCode.F9))
            {
                TakeScreenshotPNG();
            }

            // Shortcut: F10 = Toggle Screen Recording (MP4)
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ToggleScreenRecording();
            }

            // Auto-save settings periodically every 5 seconds
            if (Time.time - lastAutoSaveTime > 5.0f)
            {
                lastAutoSaveTime = Time.time;
                SaveAllPersistentSettings();
            }

            if (targetTransform == null)
            {
                if (Time.time - lastSearchTime > 2.0f)
                {
                    lastSearchTime = Time.time;
                    FindTargetCamera();
                }
                return;
            }

            // Keyboard movement in World Space (WASD + Q/E/Space/Ctrl)
            float currentSpeed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? 3.5f : 1f);
            Vector3 moveDir = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) moveDir += targetTransform.forward;
            if (Input.GetKey(KeyCode.S)) moveDir -= targetTransform.forward;
            if (Input.GetKey(KeyCode.A)) moveDir -= targetTransform.right;
            if (Input.GetKey(KeyCode.D)) moveDir += targetTransform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) moveDir += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) moveDir -= Vector3.up;

            if (moveDir != Vector3.zero || isRightClickDragging)
            {
                if (currentFollowMode != FollowMode.FreeFlight_World)
                {
                    currentFollowMode = FollowMode.FreeFlight_World;
                }
            }

            if (moveDir != Vector3.zero)
            {
                pos += moveDir * currentSpeed * Time.deltaTime;
            }

            // Right-click drag to rotate camera
            if (Input.GetMouseButtonDown(1))
            {
                isRightClickDragging = true;
                lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isRightClickDragging = false;
            }

            if (isRightClickDragging)
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                lastMousePos = Input.mousePosition;

                rot.x -= delta.y * lookSensitivity * 0.1f;
                rot.y += delta.x * lookSensitivity * 0.1f;
            }
        }

        private void LateUpdate()
        {
            ApplyTransformAndSettings();
            UpdateShapeTransform();
        }

        private void OnApplicationQuit()
        {
            SaveAllPersistentSettings();
        }

        private void OnDisable()
        {
            SaveAllPersistentSettings();
        }

        private void OnDestroy()
        {
            SaveAllPersistentSettings();
            if (isRecording)
            {
                StopRecordingMP4();
            }
            if (shapeObject != null)
            {
                Destroy(shapeObject);
            }
        }

        public Transform GetMainVRCamera()
        {
            if (cachedMainVRCamera != null && cachedMainVRCamera.gameObject != null && cachedMainVRCamera.gameObject.activeInHierarchy)
            {
                return cachedMainVRCamera;
            }

            // 1. Look for CenterEyeAnchor (Oculus / Meta XR / OpenXR standard)
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null && centerEye.transform != targetTransform)
            {
                cachedMainVRCamera = centerEye.transform;
                return cachedMainVRCamera;
            }

            // 2. Check all cameras with stereoTargetEye != None (VR Headset Lenses)
            foreach (var cam in Camera.allCameras)
            {
                if (cam != null && cam.transform != targetTransform && cam.stereoTargetEye != StereoTargetEyeMask.None && cam.enabled)
                {
                    cachedMainVRCamera = cam.transform;
                    return cachedMainVRCamera;
                }
            }

            // 3. Look for standard VR hierarchy names
            string[] candidateNames = new string[] {
                "Main Camera", "Head", "VRCamera", "XR Camera", "FirstPersonCamera", 
                "PlayerCamera", "EyeCamera", "Camera (eye)", "TrackedPoseDriver"
            };

            foreach (var cName in candidateNames)
            {
                var found = GameObject.Find(cName);
                if (found != null && found.transform != targetTransform)
                {
                    cachedMainVRCamera = found.transform;
                    return cachedMainVRCamera;
                }
            }

            // 4. Look for Camera.main
            if (Camera.main != null && Camera.main.transform != targetTransform)
            {
                cachedMainVRCamera = Camera.main.transform;
                return cachedMainVRCamera;
            }

            // 5. Look for tagged MainCamera
            var tagged = GameObject.FindWithTag("MainCamera");
            if (tagged != null && tagged.transform != targetTransform)
            {
                cachedMainVRCamera = tagged.transform;
                return cachedMainVRCamera;
            }

            return null;
        }

        private void ApplyTransformAndSettings()
        {
            if (targetTransform == null) return;

            Vector3 targetPos = pos;
            Quaternion targetRot = Quaternion.Euler(rot);

            Transform vrHead = GetMainVRCamera();

            if (currentFollowMode == FollowMode.FirstPerson_1to1)
            {
                if (vrHead != null)
                {
                    targetPos = vrHead.position;
                    targetRot = vrHead.rotation;

                    pos = targetPos;
                    rot = targetRot.eulerAngles;
                }
            }
            else if (currentFollowMode == FollowMode.ThirdPerson_Shoulder)
            {
                if (vrHead != null)
                {
                    Vector3 headForward = vrHead.forward;
                    headForward.y = 0;
                    if (headForward.sqrMagnitude < 0.001f) headForward = Vector3.forward;
                    headForward.Normalize();

                    Vector3 targetThirdPersonPos = vrHead.position - headForward * thirdPersonDistance + Vector3.up * thirdPersonHeight;
                    Vector3 lookTarget = vrHead.position + Vector3.up * (thirdPersonHeight * 0.5f);
                    Quaternion targetThirdPersonRot = Quaternion.LookRotation(lookTarget - targetThirdPersonPos);

                    targetPos = targetThirdPersonPos;
                    targetRot = targetThirdPersonRot;

                    pos = targetPos;
                    rot = targetRot.eulerAngles;
                }
            }
            else // FreeFlight_World
            {
                if (targetTransform.parent != null)
                {
                    originalParent = targetTransform.parent;
                    targetTransform.SetParent(null, true);
                    isUnparented = true;
                }

                targetPos = pos;
                targetRot = Quaternion.Euler(rot);
            }

            // Apply Video Stabilization & Steadicam Smooth Damping
            if (enableStabilization)
            {
                if (!isStabilizationInit)
                {
                    smoothCurrentPos = targetPos;
                    smoothCurrentRot = targetRot;
                    isStabilizationInit = true;
                }

                float smoothT = Mathf.Clamp01(stabilizationSmoothness * Time.unscaledDeltaTime);
                smoothCurrentPos = Vector3.Lerp(smoothCurrentPos, targetPos, smoothT);
                smoothCurrentRot = Quaternion.Slerp(smoothCurrentRot, targetRot, smoothT);

                targetTransform.position = smoothCurrentPos;
                targetTransform.rotation = smoothCurrentRot;
            }
            else
            {
                smoothCurrentPos = targetPos;
                smoothCurrentRot = targetRot;
                targetTransform.position = targetPos;
                targetTransform.rotation = targetRot;
            }

            if (targetCamera != null)
            {
                targetCamera.fieldOfView = fov;
                targetCamera.farClipPlane = Mathf.Max(targetCamera.farClipPlane, 2000f);

                if (disableSkybox)
                {
                    targetCamera.clearFlags = CameraClearFlags.SolidColor;
                    targetCamera.backgroundColor = bgColor;
                }
                else
                {
                    targetCamera.clearFlags = originalClearFlags;
                }
            }
        }

        public void FindTargetCamera()
        {
            GameObject go = GameObject.Find("SpectatorEyeAnchor");
            if (go == null) go = GameObject.Find("SpectatorAnchor");
            if (go == null) go = GameObject.Find("SpectatorCamera");

            if (go == null && Camera.allCamerasCount > 0)
            {
                foreach (var cam in Camera.allCameras)
                {
                    if (cam != null && cam.name != null && cam.name.IndexOf("Spectator", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        go = cam.gameObject;
                        break;
                    }
                }
            }

            // --- UNIVERSAL FALLBACK: IF NO SPECTATOR CAMERA EXISTS IN THE GAME AT ALL ---
            if (go == null)
            {
                // Create a dynamic, dedicated Spectator Camera for PC screen rendering!
                GameObject createdCamObj = new GameObject("[Universal_SpectatorCamera]");
                DontDestroyOnLoad(createdCamObj);
                Camera specCam = createdCamObj.AddComponent<Camera>();
                
                // Critical settings for VR Desktop Spectator:
                specCam.stereoTargetEye = StereoTargetEyeMask.None; // Render to PC monitor window, NOT into VR headset lenses!
                specCam.depth = 999; // Top depth so it renders onto the desktop screen
                specCam.cullingMask = ~0; // Everything
                specCam.fieldOfView = 60f;
                specCam.farClipPlane = 2000f;
                specCam.nearClipPlane = 0.05f;
                specCam.clearFlags = CameraClearFlags.Skybox;

                // Position it at player's head or slightly behind initially
                Transform vrHead = GetMainVRCamera();
                if (vrHead != null)
                {
                    createdCamObj.transform.position = vrHead.position;
                    createdCamObj.transform.rotation = vrHead.rotation;
                }
                else
                {
                    createdCamObj.transform.position = new Vector3(0, 1.5f, 0);
                    createdCamObj.transform.rotation = Quaternion.identity;
                }

                go = createdCamObj;
                Debug.Log("[UniversalSpectator] Created dynamic Universal Spectator Camera for PC display!");
            }

            if (go != null)
            {
                targetTransform = go.transform;
                targetName = go.name;
                targetCamera = go.GetComponent<Camera>();
                if (targetCamera == null) targetCamera = go.GetComponentInChildren<Camera>();

                if (targetCamera != null)
                {
                    originalClearFlags = targetCamera.clearFlags;
                    if (!isSettingsLoaded) bgColor = targetCamera.backgroundColor;
                    if (!isSettingsLoaded) fov = targetCamera.fieldOfView;
                    targetCamera.farClipPlane = Mathf.Max(targetCamera.farClipPlane, 2000f);
                }

                // Disable competing spectator scripts on this GameObject
                var allComponents = go.GetComponents<MonoBehaviour>();
                foreach (var c in allComponents)
                {
                    if (c != null && c != this)
                    {
                        string typeName = c.GetType().Name;
                        if (typeName.Contains("Spectator") || typeName.Contains("Dome"))
                        {
                            c.enabled = false;
                        }
                    }
                }

                if (!isPosInitialized)
                {
                    if (!isSettingsLoaded)
                    {
                        pos = targetTransform.position;
                        rot = targetTransform.eulerAngles;
                    }
                    isPosInitialized = true;

                    if (!isShapePlacedInWorld)
                    {
                        shapeWorldPos = targetTransform.position + targetTransform.forward * 8f;
                        shapeWorldRot = new Vector3(0f, targetTransform.eulerAngles.y, 0f);
                        isShapePlacedInWorld = true;
                    }
                }

                Debug.Log($"[SpectatorCameraController] Target spectator camera bound: {targetName}");
            }
        }

        // ==========================================
        // 💾 FULL PERSISTENCE ENGINE (RESTORE ON REOPEN)
        // ==========================================

        private string GetSettingsFilePath()
        {
            string capturesPath = Path.Combine(GetCapturesRoot(), "SpectatorSettings.ini");
            return capturesPath;
        }

        public void SaveAllPersistentSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                // Camera
                sb.AppendLine($"camPosX={pos.x.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"camPosY={pos.y.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"camPosZ={pos.z.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"camRotX={rot.x.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"camRotY={rot.y.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"camRotZ={rot.z.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"fov={fov.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"moveSpeed={moveSpeed.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"followMode={(int)currentFollowMode}");
                sb.AppendLine($"thirdPersonDistance={thirdPersonDistance.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"thirdPersonHeight={thirdPersonHeight.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"isPosInitialized={isPosInitialized}");

                // Solid Color / Skybox
                sb.AppendLine($"disableSkybox={disableSkybox}");
                sb.AppendLine($"bgR={bgColor.r.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"bgG={bgColor.g.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"bgB={bgColor.b.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"bgA={bgColor.a.ToString("R", CultureInfo.InvariantCulture)}");

                // 3D Shape
                sb.AppendLine($"show3DShape={show3DShape}");
                sb.AppendLine($"shapeType={(int)currentShape}");
                sb.AppendLine($"shapeImagePath={loadedShapeFilePath}");
                sb.AppendLine($"shapePosX={shapeWorldPos.x.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapePosY={shapeWorldPos.y.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapePosZ={shapeWorldPos.z.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeRotX={shapeWorldRot.x.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeRotY={shapeWorldRot.y.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeRotZ={shapeWorldRot.z.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeScaleX={shapeScale.x.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeScaleY={shapeScale.y.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeScaleZ={shapeScale.z.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"bendSidesAngle={bendSidesAngle.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"bendTopBottomAngle={bendTopBottomAngle.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"shapeBrightness={shapeBrightness.ToString("R", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"isShapePlacedInWorld={isShapePlacedInWorld}");

                // Presets (Camera Only)
                for (int i = 0; i < 4; i++)
                {
                    sb.AppendLine($"p{i}_saved={presets[i].isSaved}");
                    sb.AppendLine($"p{i}_posX={presets[i].posX.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_posY={presets[i].posY.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_posZ={presets[i].posZ.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_rotX={presets[i].rotX.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_rotY={presets[i].rotY.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_rotZ={presets[i].rotZ.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_fov={presets[i].fov.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_moveSpeed={presets[i].moveSpeed.ToString("R", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"p{i}_lockWorldPos={presets[i].lockWorldPos}");
                }

                // Quick inputs
                sb.AppendLine($"inputLevelStr={inputLevelStr}");
                sb.AppendLine($"inputCoinsStr={inputCoinsStr}");
                sb.AppendLine($"inputLivesStr={inputLivesStr}");
                sb.AppendLine($"inputStarsStr={inputStarsStr}");

                // Video Recording Performance & Stabilization Settings
                sb.AppendLine($"videoResMode={(int)videoResMode}");
                sb.AppendLine($"targetRecordFPS={targetRecordFPS}");
                sb.AppendLine($"videoQuality={(int)videoQuality}");
                sb.AppendLine($"enableStabilization={enableStabilization}");
                sb.AppendLine($"stabilizationSmoothness={stabilizationSmoothness.ToString("R", CultureInfo.InvariantCulture)}");

                string content = sb.ToString();
                File.WriteAllText(GetSettingsFilePath(), content);
            }
            catch { }
        }

        public void LoadAllPersistentSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (!File.Exists(path)) return;

                var lines = File.ReadAllLines(path);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in lines)
                {
                    int idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        string key = line.Substring(0, idx).Trim();
                        string val = line.Substring(idx + 1).Trim();
                        dict[key] = val;
                    }
                }

                float fVal;
                bool bVal;
                int iVal;

                if (dict.TryGetValue("camPosX", out var s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) pos.x = fVal;
                if (dict.TryGetValue("camPosY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) pos.y = fVal;
                if (dict.TryGetValue("camPosZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) pos.z = fVal;
                if (dict.TryGetValue("camRotX", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) rot.x = fVal;
                if (dict.TryGetValue("camRotY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) rot.y = fVal;
                if (dict.TryGetValue("camRotZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) rot.z = fVal;
                if (dict.TryGetValue("fov", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) fov = fVal;
                if (dict.TryGetValue("moveSpeed", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) moveSpeed = fVal;
                if (dict.TryGetValue("followMode", out s) && int.TryParse(s, out iVal)) currentFollowMode = (FollowMode)iVal;
                if (dict.TryGetValue("thirdPersonDistance", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) thirdPersonDistance = fVal;
                if (dict.TryGetValue("thirdPersonHeight", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) thirdPersonHeight = fVal;
                if (dict.TryGetValue("isPosInitialized", out s) && bool.TryParse(s, out bVal)) isPosInitialized = bVal;

                if (dict.TryGetValue("disableSkybox", out s) && bool.TryParse(s, out bVal)) disableSkybox = bVal;
                if (dict.TryGetValue("bgR", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) bgColor.r = fVal;
                if (dict.TryGetValue("bgG", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) bgColor.g = fVal;
                if (dict.TryGetValue("bgB", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) bgColor.b = fVal;
                if (dict.TryGetValue("bgA", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) bgColor.a = fVal;

                if (dict.TryGetValue("show3DShape", out s) && bool.TryParse(s, out bVal)) show3DShape = bVal;
                if (dict.TryGetValue("shapeType", out s) && int.TryParse(s, out iVal)) currentShape = (Shape3DType)iVal;
                if (dict.TryGetValue("shapePosX", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) shapeWorldPos.x = fVal;
                if (dict.TryGetValue("shapePosY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) shapeWorldPos.y = fVal;
                if (dict.TryGetValue("shapePosZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) shapeWorldPos.z = fVal;
                if (dict.TryGetValue("shapeRotX", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) shapeWorldRot.x = fVal;
                if (dict.TryGetValue("shapeRotY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) shapeWorldRot.y = fVal;
                if (dict.TryGetValue("shapeRotZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) shapeWorldRot.z = fVal;
                if (dict.TryGetValue("shapeScaleX", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) shapeScale.x = fVal;
                if (dict.TryGetValue("shapeScaleY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) shapeScale.y = fVal;
                if (dict.TryGetValue("shapeScaleZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) shapeScale.z = fVal;
                if (dict.TryGetValue("bendSidesAngle", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) bendSidesAngle = fVal;
                if (dict.TryGetValue("bendTopBottomAngle", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) bendTopBottomAngle = fVal;
                if (dict.TryGetValue("shapeBrightness", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) shapeBrightness = fVal;
                if (dict.TryGetValue("isShapePlacedInWorld", out s) && bool.TryParse(s, out bVal)) isShapePlacedInWorld = bVal;

                for (int i = 0; i < 4; i++)
                {
                    if (dict.TryGetValue($"p{i}_saved", out s) && bool.TryParse(s, out bVal)) presets[i].isSaved = bVal;
                    if (dict.TryGetValue($"p{i}_posX", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) presets[i].posX = fVal;
                    if (dict.TryGetValue($"p{i}_posY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) presets[i].posY = fVal;
                    if (dict.TryGetValue($"p{i}_posZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) presets[i].posZ = fVal;
                    if (dict.TryGetValue($"p{i}_rotX", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) presets[i].rotX = fVal;
                    if (dict.TryGetValue($"p{i}_rotY", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) presets[i].rotY = fVal;
                    if (dict.TryGetValue($"p{i}_rotZ", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) presets[i].rotZ = fVal;
                    if (dict.TryGetValue($"p{i}_fov", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) presets[i].fov = fVal;
                    if (dict.TryGetValue($"p{i}_moveSpeed", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal) && fVal > 0) presets[i].moveSpeed = fVal;
                    if (dict.TryGetValue($"p{i}_lockWorldPos", out s) && bool.TryParse(s, out bVal)) presets[i].lockWorldPos = bVal;
                }

                if (dict.TryGetValue("inputLevelStr", out s) && !string.IsNullOrEmpty(s)) inputLevelStr = s;
                if (dict.TryGetValue("inputCoinsStr", out s) && !string.IsNullOrEmpty(s)) inputCoinsStr = s;
                if (dict.TryGetValue("inputLivesStr", out s) && !string.IsNullOrEmpty(s)) inputLivesStr = s;
                if (dict.TryGetValue("inputStarsStr", out s) && !string.IsNullOrEmpty(s)) inputStarsStr = s;

                if (dict.TryGetValue("videoResMode", out s) && int.TryParse(s, out iVal)) videoResMode = (VideoResMode)iVal;
                if (dict.TryGetValue("targetRecordFPS", out s) && int.TryParse(s, out iVal)) targetRecordFPS = Mathf.Clamp(iVal, 15, 120);
                if (dict.TryGetValue("videoQuality", out s) && int.TryParse(s, out iVal)) videoQuality = (VideoQualityMode)iVal;
                if (dict.TryGetValue("enableStabilization", out s) && bool.TryParse(s, out bVal)) enableStabilization = bVal;
                if (dict.TryGetValue("stabilizationSmoothness", out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal)) stabilizationSmoothness = Mathf.Clamp(fVal, 1.0f, 30.0f);

                if (dict.TryGetValue("shapeImagePath", out s) && !string.IsNullOrEmpty(s) && File.Exists(s))
                {
                    LoadShapeFile(s);
                }

                isSettingsLoaded = true;
                Debug.Log("[SpectatorCameraController] Persistent settings loaded successfully from INI!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SpectatorCameraController] Could not load settings: {ex.Message}");
            }
        }

        // ==========================================
        // 🧱 3D PHYSICAL SCENE OBJECTS (TRUE 3D PARALLAX)
        // ==========================================

        private Shader GetBestAvailableShader()
        {
            var sceneRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (var r in sceneRenderers)
            {
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.isSupported)
                {
                    string sname = r.sharedMaterial.shader.name;
                    if (sname.Contains("Unlit") || sname.Contains("Universal Render Pipeline/Unlit") || sname.Contains("Sprites/Default") || sname.Contains("UI/Default"))
                    {
                        return r.sharedMaterial.shader;
                    }
                }
            }

            string[] candidateShaders = new string[] {
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Lit",
                "Unlit/Texture",
                "Sprites/Default",
                "UI/Default",
                "Mobile/Unlit (Supports Lightmap)",
                "Standard"
            };

            foreach (var s in candidateShaders)
            {
                Shader found = Shader.Find(s);
                if (found != null && found.isSupported) return found;
            }

            foreach (var r in sceneRenderers)
            {
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.isSupported)
                {
                    return r.sharedMaterial.shader;
                }
            }

            return Shader.Find("Standard");
        }

        private void EnsureShapeObject()
        {
            if (shapeObject == null)
            {
                shapeObject = new GameObject("[Spectator_3D_Shape]");
                DontDestroyOnLoad(shapeObject);
                shapeObject.layer = 0; // Default scene layer

                MeshFilter mf = shapeObject.AddComponent<MeshFilter>();
                shapeRenderer = shapeObject.AddComponent<MeshRenderer>();
                shapeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shapeRenderer.receiveShadows = false;

                Shader shader = GetBestAvailableShader();
                shapeMaterial = new Material(shader);
                shapeMaterial.renderQueue = 2000; // Geometry render queue
                shapeRenderer.material = shapeMaterial;

                RebuildShapeMesh();
            }
        }

        private void RebuildShapeMesh()
        {
            if (shapeObject == null) return;
            MeshFilter mf = shapeObject.GetComponent<MeshFilter>();
            if (mf == null) return;

            if (currentShape == Shape3DType.Sphere360HDRI)
            {
                mf.mesh = Generate360SphereMesh(48, 96);
            }
            else if (currentShape == Shape3DType.CurvedBackdrop3D)
            {
                mf.mesh = GenerateCurvedBackdropMesh(48, 32, bendSidesAngle, bendTopBottomAngle);
            }
            else
            {
                mf.mesh = GenerateCurvedBackdropMesh(4, 4, 0f, 0f);
            }
        }

        private Mesh GenerateCurvedBackdropMesh(int resX, int resY, float bendAngleX, float bendAngleY)
        {
            Mesh mesh = new Mesh();
            mesh.name = "3D_CurvedBackdropMesh";

            int numVertices = (resX + 1) * (resY + 1);
            Vector3[] vertices = new Vector3[numVertices * 2]; // double-sided
            Vector2[] uvs = new Vector2[numVertices * 2];
            int[] triangles = new int[resX * resY * 6 * 2];

            float radX = bendAngleX * Mathf.Deg2Rad;
            float radY = bendAngleY * Mathf.Deg2Rad;

            int v = 0;
            for (int y = 0; y <= resY; y++)
            {
                float vCoord = (float)y / resY;
                float normY = vCoord - 0.5f; // [-0.5, 0.5]

                for (int x = 0; x <= resX; x++)
                {
                    float uCoord = (float)x / resX;
                    float normX = uCoord - 0.5f; // [-0.5, 0.5]

                    float posX, posY, posZ;

                    if (radX < 0.001f && radY < 0.001f)
                    {
                        // Flat Plane
                        posX = normX;
                        posY = normY;
                        posZ = 0f;
                    }
                    else
                    {
                        // Dual 3D Curvature (Horizontal Studio Cyc + Vertical Bowl)
                        float angleX = normX * radX;
                        float angleY = normY * radY;

                        float rX = (radX > 0.001f) ? 1.0f / radX : 1.0f;
                        float rY = (radY > 0.001f) ? 1.0f / radY : 1.0f;

                        float sinX = Mathf.Sin(angleX);
                        float cosX = Mathf.Cos(angleX);
                        float sinY = Mathf.Sin(angleY);
                        float cosY = Mathf.Cos(angleY);

                        if (radX > 0.001f && radY > 0.001f)
                        {
                            posX = rX * sinX * cosY;
                            posY = rY * sinY;
                            posZ = -(rX * (1.0f - cosX * cosY));
                        }
                        else if (radX > 0.001f)
                        {
                            posX = rX * sinX;
                            posY = normY;
                            posZ = -(rX * (1.0f - cosX));
                        }
                        else
                        {
                            posX = normX;
                            posY = rY * sinY;
                            posZ = -(rY * (1.0f - cosY));
                        }
                    }

                    vertices[v] = new Vector3(posX, posY, posZ);
                    uvs[v] = new Vector2(uCoord, vCoord);
                    v++;
                }
            }

            // Duplicate vertices for back side
            int offset = numVertices;
            for (int i = 0; i < numVertices; i++)
            {
                vertices[offset + i] = vertices[i];
                uvs[offset + i] = uvs[i];
            }

            int t = 0;
            // Front triangles
            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int current = y * (resX + 1) + x;
                    int next = current + resX + 1;

                    triangles[t++] = current;
                    triangles[t++] = next;
                    triangles[t++] = current + 1;

                    triangles[t++] = current + 1;
                    triangles[t++] = next;
                    triangles[t++] = next + 1;
                }
            }

            // Back triangles
            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int current = offset + y * (resX + 1) + x;
                    int next = current + resX + 1;

                    triangles[t++] = current;
                    triangles[t++] = current + 1;
                    triangles[t++] = next;

                    triangles[t++] = current + 1;
                    triangles[t++] = next + 1;
                    triangles[t++] = next;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh Generate360SphereMesh(int latLines, int longLines)
        {
            Mesh mesh = new Mesh();
            mesh.name = "3D_EnvironmentSphere360";

            int numVertices = (latLines + 1) * (longLines + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[latLines * longLines * 6];

            int v = 0;
            for (int lat = 0; lat <= latLines; lat++)
            {
                float a1 = (float)lat / latLines * Mathf.PI;
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= longLines; lon++)
                {
                    float a2 = (float)lon / longLines * Mathf.PI * 2f;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[v] = new Vector3(sin1 * cos2, cos1, sin1 * sin2);
                    // Equirectangular 360 UV mapping
                    uvs[v] = new Vector2(1f - (float)lon / longLines, 1f - (float)lat / latLines);
                    v++;
                }
            }

            int t = 0;
            for (int lat = 0; lat < latLines; lat++)
            {
                for (int lon = 0; lon < longLines; lon++)
                {
                    int current = lat * (longLines + 1) + lon;
                    int next = current + longLines + 1;

                    // Inverted winding so triangles face inside towards camera
                    triangles[t++] = current;
                    triangles[t++] = current + 1;
                    triangles[t++] = next;

                    triangles[t++] = next;
                    triangles[t++] = current + 1;
                    triangles[t++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void UpdateShapeTransform()
        {
            if (shapeObject == null) return;

            shapeObject.SetActive(show3DShape && loadedShapeTexture != null);
            if (!shapeObject.activeSelf) return;

            if (shapeMaterial != null)
            {
                Color tint = new Color(shapeBrightness, shapeBrightness, shapeBrightness, 1f);
                if (shapeMaterial.HasProperty("_Color")) shapeMaterial.SetColor("_Color", tint);
                if (shapeMaterial.HasProperty("_BaseColor")) shapeMaterial.SetColor("_BaseColor", tint);
            }

            // Fixed in 3D World Space (True 3D Parallax relative to spectator camera & gameboard)
            shapeObject.transform.position = shapeWorldPos;
            shapeObject.transform.rotation = Quaternion.Euler(shapeWorldRot);

            float aspect = (loadedShapeTexture != null && loadedShapeTexture.height > 0)
                         ? (float)loadedShapeTexture.width / loadedShapeTexture.height
                         : 1.777f;

            if (currentShape == Shape3DType.Sphere360HDRI)
            {
                shapeObject.transform.localScale = new Vector3(shapeScale.x, shapeScale.x, shapeScale.x);
            }
            else
            {
                shapeObject.transform.localScale = new Vector3(shapeScale.x * aspect, shapeScale.y, shapeScale.z);
            }
        }

        public void Place3DShapeInFrontOfCamera()
        {
            if (targetTransform != null)
            {
                // Place 3D object 8 meters in front of current camera position in 3D world space
                shapeWorldPos = targetTransform.position + targetTransform.forward * 8f;
                shapeWorldRot = new Vector3(0f, targetTransform.eulerAngles.y, 0f);
                isShapePlacedInWorld = true;
                terminalLog += $"\n[3D OBJECT] Placed 3D Shape at World Pos: {shapeWorldPos}";
                ShowNotice("🎯 Obiect 3D Poziționat în Spatele Tablei");
                SaveAllPersistentSettings();
            }
        }

        public void LoadShapeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            try
            {
                EnsureShapeObject();

                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                byte[] fileBytes = File.ReadAllBytes(filePath);

                if (loadedShapeTexture != null) Destroy(loadedShapeTexture);

                if (ext == ".hdr")
                {
                    loadedShapeTexture = LoadRadianceHDRTexture(fileBytes);
                    if (loadedShapeTexture == null)
                    {
                        terminalLog += "\n[HDR ERROR] Could not decode HDR. Trying standard loader...";
                        loadedShapeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        DecodeImageToTexture(loadedShapeTexture, fileBytes);
                    }
                    currentShape = Shape3DType.Sphere360HDRI;
                    if (shapeScale.x < 50f) shapeScale = new Vector3(300f, 300f, 300f);
                }
                else
                {
                    loadedShapeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    DecodeImageToTexture(loadedShapeTexture, fileBytes);

                    float ratio = (float)loadedShapeTexture.width / Mathf.Max(1, loadedShapeTexture.height);
                    if (ratio >= 1.9f)
                    {
                        currentShape = Shape3DType.Sphere360HDRI;
                        if (shapeScale.x < 50f) shapeScale = new Vector3(300f, 300f, 300f);
                    }
                    else
                    {
                        currentShape = Shape3DType.CurvedBackdrop3D;
                        if (shapeScale.x > 100f) shapeScale = new Vector3(25f, 15f, 25f);
                    }
                }

                RebuildShapeMesh();

                if (shapeMaterial != null && loadedShapeTexture != null)
                {
                    if (shapeMaterial.HasProperty("_MainTex")) shapeMaterial.SetTexture("_MainTex", loadedShapeTexture);
                    if (shapeMaterial.HasProperty("_BaseMap")) shapeMaterial.SetTexture("_BaseMap", loadedShapeTexture);
                }

                loadedShapeFilePath = filePath;
                loadedShapeFileName = Path.GetFileName(filePath);
                show3DShape = true;

                terminalLog += $"\n[3D OBJECT] Loaded 3D Shape: {loadedShapeFileName} ({loadedShapeTexture.width}x{loadedShapeTexture.height}) -> {currentShape}";
                ShowNotice($"🌌 Obiect 3D Încărcat în Scenă: {loadedShapeFileName}");
                SaveAllPersistentSettings();
            }
            catch (Exception ex)
            {
                terminalLog += $"\n[3D OBJECT ERROR] {ex.Message}";
            }
        }

        public void ClearShapeObject()
        {
            show3DShape = false;
            loadedShapeFilePath = "";
            loadedShapeFileName = "";
            if (shapeObject != null) shapeObject.SetActive(false);
            terminalLog += "\n[3D OBJECT] 3D Shape Cleared from Scene.";
            ShowNotice("🧹 Obiect 3D Eliminat");
            SaveAllPersistentSettings();
        }

        private void PromptSelectShapeFile()
        {
            var thread = new Thread(() =>
            {
                try
                {
                    string initDir = GetCustomSkyboxesFolder();
                    string script = $"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.OpenFileDialog; $f.InitialDirectory = '{initDir}'; $f.Filter = 'Toate Formatele (*.hdr;*.jpg;*.jpeg;*.png;*.bmp)|*.hdr;*.jpg;*.jpeg;*.png;*.bmp|Imagini HDRI (*.hdr)|*.hdr|Imagini Normale (*.jpg;*.png)|*.jpg;*.png|All (*.*)|*.*'; if ($f.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {{ Write-Host $f.FileName }}";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var proc = Process.Start(psi))
                    {
                        string output = proc.StandardOutput.ReadToEnd()?.Trim();
                        proc.WaitForExit(60000);

                        if (!string.IsNullOrEmpty(output) && File.Exists(output))
                        {
                            MainThreadDispatcher.Enqueue(() => LoadShapeFile(output));
                        }
                    }
                }
                catch (Exception ex)
                {
                    terminalLog += $"\n[DIALOG ERROR] {ex.Message}";
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        // ==========================================
        // 🔮 PURE C# RADIANCE .HDR RGBE PARSER
        // ==========================================

        private static string ReadAsciiLine(BinaryReader reader)
        {
            StringBuilder sb = new StringBuilder();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte b = reader.ReadByte();
                if (b == '\n') break;
                if (b != '\r') sb.Append((char)b);
            }
            return sb.Length > 0 ? sb.ToString() : "";
        }

        private static Texture2D LoadRadianceHDRTexture(byte[] fileData)
        {
            try
            {
                if (fileData == null || fileData.Length < 16) return null;

                using (var ms = new MemoryStream(fileData))
                using (var reader = new BinaryReader(ms))
                {
                    int width = 0, height = 0;
                    string line;
                    int linesRead = 0;

                    while (ms.Position < ms.Length && linesRead < 100)
                    {
                        line = ReadAsciiLine(reader).Trim();
                        linesRead++;

                        if (line.StartsWith("-Y") || line.StartsWith("+Y") || line.Contains("-Y") || line.Contains("+Y"))
                        {
                            var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if ((parts[i] == "-Y" || parts[i] == "+Y") && i + 1 < parts.Length)
                                {
                                    int.TryParse(parts[i + 1], out height);
                                }
                                if ((parts[i] == "+X" || parts[i] == "-X") && i + 1 < parts.Length)
                                {
                                    int.TryParse(parts[i + 1], out width);
                                }
                            }
                            if (width > 0 && height > 0) break;
                        }
                    }

                    if (width <= 0 || height <= 0 || width > 16384 || height > 16384) return null;

                    Color32[] pixels = new Color32[width * height];
                    byte[] scanline = new byte[width * 4];

                    for (int y = 0; y < height; y++)
                    {
                        int row = height - 1 - y; // vertical flip for Unity

                        if (ms.Position + 4 > ms.Length) break;
                        byte b0 = reader.ReadByte();
                        byte b1 = reader.ReadByte();
                        byte b2 = reader.ReadByte();
                        byte b3 = reader.ReadByte();

                        if (b0 == 2 && b1 == 2 && ((b2 << 8) | b3) == width)
                        {
                            // Radiance RLE encoding
                            for (int c = 0; c < 4; c++)
                            {
                                int x = 0;
                                while (x < width && ms.Position < ms.Length)
                                {
                                    byte count = reader.ReadByte();
                                    if (count > 128)
                                    {
                                        int len = count - 128;
                                        byte val = reader.ReadByte();
                                        for (int i = 0; i < len && x < width; i++) scanline[(x++) * 4 + c] = val;
                                    }
                                    else
                                    {
                                        int len = count;
                                        for (int i = 0; i < len && x < width && ms.Position < ms.Length; i++) scanline[(x++) * 4 + c] = reader.ReadByte();
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Uncompressed legacy format
                            scanline[0] = b0; scanline[1] = b1; scanline[2] = b2; scanline[3] = b3;
                            for (int x = 1; x < width && ms.Position + 4 <= ms.Length; x++)
                            {
                                for (int c = 0; c < 4; c++) scanline[x * 4 + c] = reader.ReadByte();
                            }
                        }

                        // Convert RGBE to Tonemapped Color32
                        for (int x = 0; x < width; x++)
                        {
                            byte r = scanline[x * 4 + 0];
                            byte g = scanline[x * 4 + 1];
                            byte b = scanline[x * 4 + 2];
                            byte e = scanline[x * 4 + 3];

                            if (e == 0)
                            {
                                pixels[row * width + x] = new Color32(0, 0, 0, 255);
                            }
                            else
                            {
                                float f = Mathf.Pow(2.0f, e - 128 - 8);
                                float rf = r * f;
                                float gf = g * f;
                                float bf = b * f;

                                // Fast Reinhard Tonemapping for vibrant HDR representation
                                byte cr = (byte)(Mathf.Clamp01(rf / (rf + 1.0f)) * 255f);
                                byte cg = (byte)(Mathf.Clamp01(gf / (gf + 1.0f)) * 255f);
                                byte cb = (byte)(Mathf.Clamp01(bf / (bf + 1.0f)) * 255f);

                                pixels[row * width + x] = new Color32(cr, cg, cb, 255);
                            }
                        }
                    }

                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    return tex;
                }
            }
            catch
            {
                return null;
            }
        }

        // ==========================================
        // 📸 SCREENSHOT & 🎥 MP4 RECORDING ENGINE
        // ==========================================

        private string GetCapturesRoot()
        {
            string projectDir = @"E:\Danny\M3AR_PROJECT\Captures";
            if (Directory.Exists(@"E:\Danny\M3AR_PROJECT"))
            {
                if (!Directory.Exists(projectDir)) Directory.CreateDirectory(projectDir);
                return projectDir;
            }

            string localDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Captures");
            if (!Directory.Exists(localDir)) Directory.CreateDirectory(localDir);
            return localDir;
        }

        private string GetScreenshotsFolder()
        {
            string folder = Path.Combine(GetCapturesRoot(), "Screenshots");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private string GetVideosFolder()
        {
            string folder = Path.Combine(GetCapturesRoot(), "Videos");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private string GetCustomSkyboxesFolder()
        {
            string folder = Path.Combine(GetCapturesRoot(), "CustomSkyboxes");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private void EnsureCaptureDirectories()
        {
            GetScreenshotsFolder();
            GetVideosFolder();
            GetCustomSkyboxesFolder();
        }

        private bool DecodeImageToTexture(Texture2D tex, byte[] data)
        {
            try
            {
                Type imgConv = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule") 
                            ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine");
                if (imgConv == null)
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "UnityEngine.ImageConversionModule" || a.GetName().Name == "UnityEngine");
                    if (asm != null) imgConv = asm.GetType("UnityEngine.ImageConversion");
                }

                if (imgConv != null)
                {
                    var m = imgConv.GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Texture2D), typeof(byte[]) }, null);
                    if (m != null)
                    {
                        return (bool)m.Invoke(null, new object[] { tex, data });
                    }
                }
            }
            catch { }

            return false;
        }

        private byte[] EncodeTextureToPNG(Texture2D tex)
        {
            try
            {
                Type imgConv = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule") 
                            ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine");
                if (imgConv == null)
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "UnityEngine.ImageConversionModule" || a.GetName().Name == "UnityEngine");
                    if (asm != null) imgConv = asm.GetType("UnityEngine.ImageConversion");
                }

                if (imgConv != null)
                {
                    var m = imgConv.GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Texture2D) }, null);
                    if (m != null)
                    {
                        return (byte[])m.Invoke(null, new object[] { tex });
                    }
                }
            }
            catch { }

            return null;
        }

        public void TakeScreenshotPNG()
        {
            takeScreenshotOnNextFrame = true;
        }

        private void ExecuteTakeScreenshot()
        {
            try
            {
                int width = Screen.width;
                int height = Screen.height;

                byte[] pngBytes = null;

                if (disableSkybox)
                {
                    // Transparent PNG mode with 8x MSAA for ultra-smooth edges
                    if (targetCamera == null) FindTargetCamera();
                    Camera cam = targetCamera ?? Camera.main;
                    if (cam != null)
                    {
                        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                        rt.antiAliasing = 8;
                        RenderTexture prevRT = cam.targetTexture;
                        RenderTexture prevActive = RenderTexture.active;

                        cam.targetTexture = rt;
                        cam.Render();

                        RenderTexture.active = rt;
                        Texture2D shot = new Texture2D(width, height, TextureFormat.ARGB32, false);
                        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        shot.Apply();

                        cam.targetTexture = prevRT;
                        RenderTexture.active = prevActive;
                        Destroy(rt);

                        pngBytes = EncodeTextureToPNG(shot);
                        Destroy(shot);
                    }
                }
                else
                {
                    // Direct crisp Windows screen capture with all post-processing & anti-aliasing
                    Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    shot.Apply();

                    pngBytes = EncodeTextureToPNG(shot);
                    Destroy(shot);
                }

                if (pngBytes != null && pngBytes.Length > 0)
                {
                    string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                    string fullPath = Path.Combine(GetScreenshotsFolder(), fileName);
                    File.WriteAllBytes(fullPath, pngBytes);

                    terminalLog += $"\n[SCREENSHOT] Saved: {fileName}";
                    ShowNotice($"📸 Screenshot Salvat: {fileName}");
                }
                else
                {
                    terminalLog += "\n[SCREENSHOT ERROR] PNG Encoder unavailable.";
                }
            }
            catch (Exception ex)
            {
                terminalLog += $"\n[SCREENSHOT ERROR] {ex.Message}";
            }
        }

        public void ToggleScreenRecording()
        {
            if (isRecording)
            {
                StopRecordingMP4();
            }
            else
            {
                StartRecordingMP4();
            }
        }

        private string FindFFmpegPath()
        {
            string[] candidates = new string[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
                @"E:\Danny\SpectatorInjectorKit\ffmpeg.exe",
                @"C:\ffmpeg\bin\ffmpeg.exe",
                "ffmpeg.exe"
            };

            foreach (var p in candidates)
            {
                if (File.Exists(p)) return p;
            }

            return "ffmpeg.exe";
        }

        public void StartRecordingMP4()
        {
            if (isRecording) return;

            try
            {
                // Always read exact full screen dimensions from Unity backbuffer (Zero Crop)
                recordWidth = Screen.width;
                recordHeight = Screen.height;

                // Ensure even dimensions for H.264
                if (recordWidth % 2 != 0) recordWidth--;
                if (recordHeight % 2 != 0) recordHeight--;

                string fileName = $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
                currentVideoPath = Path.Combine(GetVideosFolder(), fileName);
                string ffmpegPath = FindFFmpegPath();

                // Build video filter: vflip (inverts bottom-up backbuffer to top-down video) + optional GPU scaling
                string vf = "vflip";
                if (videoResMode == VideoResMode.FullHD_1080p && (recordWidth != 1920 || recordHeight != 1080))
                {
                    vf = "vflip,scale=1920:1080:flags=fast_bilinear";
                }
                else if (videoResMode == VideoResMode.HD_720p && (recordWidth != 1280 || recordHeight != 720))
                {
                    vf = "vflip,scale=1280:720:flags=fast_bilinear";
                }

                // Select encoder tuning for zero latency & maximum speed
                string nvencPreset = "p1";
                string nvencCQ = "22";
                string x264Preset = "ultrafast";
                string x264CRF = "22";

                if (videoQuality == VideoQualityMode.HighQuality)
                {
                    nvencPreset = "p3";
                    nvencCQ = "18";
                    x264Preset = "faster";
                    x264CRF = "18";
                }
                else if (videoQuality == VideoQualityMode.Balanced)
                {
                    nvencPreset = "p2";
                    nvencCQ = "20";
                    x264Preset = "veryfast";
                    x264CRF = "20";
                }
                else // UltraFast_ZeroLag
                {
                    nvencPreset = "p1";
                    nvencCQ = "22";
                    x264Preset = "ultrafast";
                    x264CRF = "22";
                }

                // Hardware NVENC uses -tune ull, CPU libx264 uses -tune zerolatency
                string args = $"-y -f rawvideo -vcodec rawvideo -s {recordWidth}x{recordHeight} -pix_fmt rgb24 -r {targetRecordFPS} -i - -vf \"{vf}\" -c:v h264_nvenc -preset {nvencPreset} -tune ull -cq {nvencCQ} -pix_fmt yuv420p -movflags +faststart \"{currentVideoPath}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                try
                {
                    ffmpegProcess = Process.Start(psi);
                    Thread.Sleep(50);
                    if (ffmpegProcess == null || ffmpegProcess.HasExited)
                    {
                        throw new Exception("NVENC initialization failed");
                    }
                }
                catch
                {
                    // Fallback to CPU libx264 ultrafast if NVENC not initialized
                    args = $"-y -f rawvideo -vcodec rawvideo -s {recordWidth}x{recordHeight} -pix_fmt rgb24 -r {targetRecordFPS} -i - -vf \"{vf}\" -c:v libx264 -preset {x264Preset} -tune zerolatency -crf {x264CRF} -pix_fmt yuv420p -movflags +faststart \"{currentVideoPath}\"";
                    psi.Arguments = args;
                    ffmpegProcess = Process.Start(psi);
                }

                if (ffmpegProcess == null || ffmpegProcess.HasExited)
                {
                    terminalLog += "\n[REC ERROR] Could not start FFmpeg process.";
                    return;
                }

                ffmpegStdin = ffmpegProcess.StandardInput.BaseStream;

                // Pre-allocate buffer pool for zero-GC memory allocation (0 allocations per frame!)
                int frameByteSize = recordWidth * recordHeight * 3;
                bufferPool = new ConcurrentQueue<byte[]>();
                for (int i = 0; i < 20; i++)
                {
                    bufferPool.Enqueue(new byte[frameByteSize]);
                }

                frameQueue = new BlockingCollection<byte[]>(boundedCapacity: 30);
                stopEncodingThread = false;

                // Background worker thread with BelowNormal priority to prevent stealing CPU from Unity
                encodingThread = new Thread(() =>
                {
                    while (!stopEncodingThread || (frameQueue != null && frameQueue.Count > 0))
                    {
                        if (frameQueue != null && frameQueue.TryTake(out byte[] frameBytes, 40))
                        {
                            try
                            {
                                if (ffmpegStdin != null)
                                {
                                    ffmpegStdin.Write(frameBytes, 0, frameBytes.Length);
                                }
                            }
                            catch { break; }
                            finally
                            {
                                // Recycle buffer back to pool for reuse (Zero GC!)
                                if (bufferPool != null) bufferPool.Enqueue(frameBytes);
                            }
                        }
                    }
                })
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                };
                encodingThread.Start();

                isRecording = true;
                recordingRealStartTime = Time.realtimeSinceStartup;
                recordedFramesCount = 0;

                string modeLabel = videoResMode == VideoResMode.FullHD_1080p ? "1080p" : (videoResMode == VideoResMode.HD_720p ? "720p" : "Native");
                terminalLog += $"\n[REC] Started Full-Frame Recording ({modeLabel} @ {targetRecordFPS} FPS): {fileName}";
                ShowNotice($"🔴 Înregistrare MP4: {modeLabel} ({targetRecordFPS} FPS)");
            }
            catch (Exception ex)
            {
                terminalLog += $"\n[REC ERROR] {ex.Message}";
                isRecording = false;
            }
        }

        public void StopRecordingMP4()
        {
            if (!isRecording) return;

            try
            {
                isRecording = false;
                stopEncodingThread = true;

                if (frameQueue != null)
                {
                    try { frameQueue.CompleteAdding(); } catch { }
                }

                if (encodingThread != null && encodingThread.IsAlive)
                {
                    encodingThread.Join(2000);
                }

                if (ffmpegStdin != null)
                {
                    try { ffmpegStdin.Flush(); } catch { }
                    try { ffmpegStdin.Close(); } catch { }
                    ffmpegStdin = null;
                }

                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    ffmpegProcess.WaitForExit(4000);
                    try { ffmpegProcess.Close(); } catch { }
                    ffmpegProcess = null;
                }

                terminalLog += $"\n[REC SUCCESS] Video Saved: {Path.GetFileName(currentVideoPath)}";
                ShowNotice($"🎬 Video MP4 Salvat cu succes!");
            }
            catch (Exception ex)
            {
                terminalLog += $"\n[REC STOP ERROR] {ex.Message}";
            }
        }

        private IEnumerator CaptureRecordCoroutine()
        {
            Texture2D recordTex = null;

            while (true)
            {
                yield return new WaitForEndOfFrame();

                MainThreadDispatcher.ProcessQueue();

                if (takeScreenshotOnNextFrame)
                {
                    takeScreenshotOnNextFrame = false;
                    ExecuteTakeScreenshot();
                }

                if (isRecording)
                {
                    float elapsedRealTime = Time.realtimeSinceStartup - recordingRealStartTime;
                    int targetFrames = Mathf.RoundToInt(elapsedRealTime * targetRecordFPS);

                    if (targetFrames > recordedFramesCount)
                    {
                        if (recordTex == null || recordTex.width != recordWidth || recordTex.height != recordHeight)
                        {
                            if (recordTex != null) Destroy(recordTex);
                            recordTex = new Texture2D(recordWidth, recordHeight, TextureFormat.RGB24, false);
                        }

                        // Read 100% full screen from backbuffer (No crop, no blit)
                        recordTex.ReadPixels(new Rect(0, 0, recordWidth, recordHeight), 0, 0);

                        // Direct zero-allocation byte copy
                        var rawSpan = recordTex.GetRawTextureData<byte>();
                        int bytesLen = rawSpan.Length;

                        while (recordedFramesCount < targetFrames && frameQueue != null && !frameQueue.IsAddingCompleted)
                        {
                            byte[] buffer = null;
                            if (bufferPool != null && bufferPool.TryDequeue(out byte[] pooled) && pooled.Length == bytesLen)
                            {
                                buffer = pooled;
                            }
                            else
                            {
                                buffer = new byte[bytesLen];
                            }

                            rawSpan.CopyTo(buffer);

                            if (!frameQueue.TryAdd(buffer, 5))
                            {
                                if (bufferPool != null) bufferPool.Enqueue(buffer);
                                break;
                            }
                            recordedFramesCount++;
                        }
                    }
                }
            }
        }

        private void ShowNotice(string text)
        {
            captureBannerNotice = text;
            captureBannerTimer = Time.time + 4.5f;
        }

        private void OpenCapturesDirectory()
        {
            string dir = GetCapturesRoot();
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                terminalLog += $"\n[ERROR] Could not open folder: {ex.Message}";
            }
        }

        // ==========================================
        // 🖥️ IMGUI INTERFACE
        // ==========================================

        private void OnGUI()
        {
            // Banner notification at top center
            if (Time.time < captureBannerTimer && !string.IsNullOrEmpty(captureBannerNotice))
            {
                GUI.color = isRecording ? new Color(1f, 0.3f, 0.3f, 0.95f) : new Color(0.2f, 0.9f, 0.3f, 0.95f);
                GUI.Box(new Rect(Screen.width / 2 - 220, 15, 440, 32), $"<b>{captureBannerNotice}</b>");
                GUI.color = Color.white;
            }

            if (!showUI) return;

            GUI.color = Color.white;
            float windowHeight = Mathf.Min(Screen.height - 40, 980);
            windowRect = GUI.Window(889911, new Rect(30, 30, 490, windowHeight), DrawControlPanel, $"🎥 Spectator Camera & Capture Suite ({VERSION}) [F1 Hide]");
        }

        private void DrawControlPanel(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 370, 25));

            GUILayout.BeginVertical();

            // Header bar with Version
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Target: <b>{(targetTransform != null ? targetName : "SEARCHING...")}</b> | <color=#00FF99><b>{VERSION}</b></color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(isMinimized ? "[ Expand ]" : "[ Min ]", GUILayout.Width(80)))
            {
                isMinimized = !isMinimized;
            }
            GUILayout.EndHorizontal();

            // Direct Action Bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Re-bind Camera"))
            {
                FindTargetCamera();
            }
            if (GUILayout.Button("💾 Save Now", GUILayout.Width(90)))
            {
                SaveAllPersistentSettings();
                ShowNotice("💾 Setări Salvate pe Disc!");
            }
            if (GUILayout.Button("📂 Captures", GUILayout.Width(90)))
            {
                OpenCapturesDirectory();
            }
            GUILayout.EndHorizontal();

            // Prominent Top Capture Buttons
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.1f, 0.6f, 1f, 1f);
            if (GUILayout.Button("📸 SCREENSHOT (F9)\n[Alpha PNG]", GUILayout.Height(38)))
            {
                TakeScreenshotPNG();
            }

            if (isRecording)
            {
                GUI.backgroundColor = new Color(1f, 0.2f, 0.2f, 1f);
                float elapsed = Time.realtimeSinceStartup - recordingRealStartTime;
                int mins = (int)(elapsed / 60);
                int secs = (int)(elapsed % 60);
                if (GUILayout.Button($"⏹ STOP REC ({mins:D2}:{secs:D2})\n[F10]", GUILayout.Height(38)))
                {
                    StopRecordingMP4();
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.1f, 0.85f, 0.3f, 1f);
                if (GUILayout.Button("🔴 RECORD VIDEO (1:1)\n[MP4 - F10]", GUILayout.Height(38)))
                {
                    StartRecordingMP4();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            if (isMinimized)
            {
                GUILayout.EndVertical();
                return;
            }

            if (targetTransform == null)
            {
                GUILayout.Label("Searching for SpectatorEyeAnchor in scene...");
                GUILayout.EndVertical();
                return;
            }

            // Scrollable view container
            mainScrollPos = GUILayout.BeginScrollView(mainScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // ==========================================
            // 🎬 1. VIDEO RECORDING & STABILIZATION SETUP
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            string resLabel = videoResMode == VideoResMode.FullHD_1080p ? "1080p" : (videoResMode == VideoResMode.HD_720p ? "720p" : "Native");
            string qualLabel = videoQuality == VideoQualityMode.UltraFast_ZeroLag ? "⚡ Zero-Lag" : (videoQuality == VideoQualityMode.Balanced ? "⚖️ Balanced" : "💎 High Quality");
            string stabLabel = enableStabilization ? "🎬 Steadicam ON" : "Off";
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>🎥 1. Video Setup & Stabilization</b> <color=#00FF99>[{resLabel} @ {targetRecordFPS} FPS | {stabLabel}]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSectionVideo ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSectionVideo = !showSectionVideo;
            }
            GUILayout.EndHorizontal();

            if (showSectionVideo)
            {
                GUILayout.Space(3);
                GUILayout.Label("<b>Recording Resolution:</b>");
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(videoResMode == VideoResMode.FullHD_1080p, " 1080p (Full HD)")) videoResMode = VideoResMode.FullHD_1080p;
                if (GUILayout.Toggle(videoResMode == VideoResMode.HD_720p, " 720p (Ultra Fast)")) videoResMode = VideoResMode.HD_720p;
                if (GUILayout.Toggle(videoResMode == VideoResMode.NativeDisplay, " Native Screen")) videoResMode = VideoResMode.NativeDisplay;
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                GUILayout.Label("<b>Target Frame Rate:</b>");
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(targetRecordFPS == 60, " 60 FPS (Silky Smooth)")) targetRecordFPS = 60;
                if (GUILayout.Toggle(targetRecordFPS == 30, " 30 FPS (Low CPU Load)")) targetRecordFPS = 30;
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                GUILayout.Label("<b>Hardware Encoding Mode:</b>");
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(videoQuality == VideoQualityMode.UltraFast_ZeroLag, " ⚡ Zero-Lag (NVENC p1)")) videoQuality = VideoQualityMode.UltraFast_ZeroLag;
                if (GUILayout.Toggle(videoQuality == VideoQualityMode.Balanced, " ⚖️ Balanced")) videoQuality = VideoQualityMode.Balanced;
                if (GUILayout.Toggle(videoQuality == VideoQualityMode.HighQuality, " 💎 High Quality")) videoQuality = VideoQualityMode.HighQuality;
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                // Video Stabilization Section
                GUILayout.BeginHorizontal();
                enableStabilization = GUILayout.Toggle(enableStabilization, " <b>🎬 Video Stabilization (Steadicam Gimbal)</b>");
                GUILayout.EndHorizontal();

                if (enableStabilization)
                {
                    GUILayout.Space(2);
                    string smoothDesc = stabilizationSmoothness >= 10f ? "Subtle (Responsive)" : (stabilizationSmoothness >= 4.5f ? "Cinematic (Steadicam)" : "Heavy (Drone/Gimbal)");
                    GUILayout.Label($"<b>Smooth Damping:</b> {stabilizationSmoothness:F1} ({smoothDesc})");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Subtle (12.0)")) { stabilizationSmoothness = 12.0f; }
                    if (GUILayout.Button("Cinematic (6.0)")) { stabilizationSmoothness = 6.0f; }
                    if (GUILayout.Button("Heavy/Drone (2.5)")) { stabilizationSmoothness = 2.5f; }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Heavy (1.5)", GUILayout.Width(70));
                    stabilizationSmoothness = GUILayout.HorizontalSlider(stabilizationSmoothness, 1.5f, 20.0f);
                    GUILayout.Label("Fast (20.0)", GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            // ==========================================
            // 👤 2. VR TRACKING & FLIGHT CONTROLS
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            string trackingStatus = currentFollowMode == FollowMode.FirstPerson_1to1 ? "1st Person 1:1" : (currentFollowMode == FollowMode.ThirdPerson_Shoulder ? "3rd Person Shoulder" : "Free Flight World");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>👤 2. VR Tracking & Free-Flight</b> <color=#00FF99>[{trackingStatus}]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSectionTracking ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSectionTracking = !showSectionTracking;
            }
            GUILayout.EndHorizontal();

            if (showSectionTracking)
            {
                GUILayout.Space(3);
                GUILayout.Label("<b>Camera Follow Mode:</b>");
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(currentFollowMode == FollowMode.FirstPerson_1to1, " 🎯 1st Person (1:1 VR)")) currentFollowMode = FollowMode.FirstPerson_1to1;
                if (GUILayout.Toggle(currentFollowMode == FollowMode.ThirdPerson_Shoulder, " 🎥 3rd Person (Shoulder)")) currentFollowMode = FollowMode.ThirdPerson_Shoulder;
                if (GUILayout.Toggle(currentFollowMode == FollowMode.FreeFlight_World, " 🛸 Free Flight (World)")) currentFollowMode = FollowMode.FreeFlight_World;
                GUILayout.EndHorizontal();

                if (currentFollowMode == FollowMode.FirstPerson_1to1)
                {
                    GUILayout.Label("<color=#00FF99>👤 <b>Tracking Player VR Headset in Real-Time (1st Person)</b>\n<i>(WASD / Mouse will switch to free flight in World Space)</i></color>");
                }
                else if (currentFollowMode == FollowMode.ThirdPerson_Shoulder)
                {
                    GUILayout.Label("<color=#00FF99>🎥 <b>Cinematic 3rd Person Over-The-Shoulder Drone Follow</b></color>");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Distance: {thirdPersonDistance:F1}m", GUILayout.Width(100));
                    thirdPersonDistance = GUILayout.HorizontalSlider(thirdPersonDistance, 0.5f, 6.0f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Height: {thirdPersonHeight:F1}m", GUILayout.Width(100));
                    thirdPersonHeight = GUILayout.HorizontalSlider(thirdPersonHeight, -0.5f, 2.5f);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("<color=#FFFF66>🔒 <b>Camera Locked in 3D World Space</b> (WASD + Mouse Flight)</color>");
                }

                GUILayout.Space(3);
                GUILayout.Label($"<b>Flight Speed:</b> {moveSpeed:F1}x  <i>(Hold Shift = Fast)</i>");
                moveSpeed = GUILayout.HorizontalSlider(moveSpeed, 0.1f, 10f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.5x Slow", GUILayout.Width(80))) moveSpeed = 0.5f;
                if (GUILayout.Button("1.0x Normal", GUILayout.Width(85))) moveSpeed = 1.0f;
                if (GUILayout.Button("2.5x Fast", GUILayout.Width(80))) moveSpeed = 2.5f;
                if (GUILayout.Button("5.0x Turbo", GUILayout.Width(80))) moveSpeed = 5.0f;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // ==========================================
            // 🎥 3. CAMERA TRANSFORM & FOV (WORLD SPACE)
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>🎥 3. Camera Position, Rotation & FOV</b> <color=#00FF99>[FOV: {fov:F0}°]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSectionCamera ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSectionCamera = !showSectionCamera;
            }
            GUILayout.EndHorizontal();

            if (showSectionCamera)
            {
                GUILayout.Space(3);
                GUILayout.Label("<b>Camera Position (World Space):</b>");

                // Position X
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                pos.x = DrawFloatField(pos.x);
                if (GUILayout.Button("-0.03", GUILayout.Width(45))) pos.x -= 0.03f;
                if (GUILayout.Button("+0.03", GUILayout.Width(45))) pos.x += 0.03f;
                if (GUILayout.Button("-0.1", GUILayout.Width(40))) pos.x -= 0.1f;
                if (GUILayout.Button("+0.1", GUILayout.Width(40))) pos.x += 0.1f;
                if (GUILayout.Button("-1", GUILayout.Width(30))) pos.x -= 1.0f;
                if (GUILayout.Button("+1", GUILayout.Width(30))) pos.x += 1.0f;
                GUILayout.EndHorizontal();
                pos.x = GUILayout.HorizontalSlider(pos.x, -10f, 10f);

                // Position Y
                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                pos.y = DrawFloatField(pos.y);
                if (GUILayout.Button("-0.03", GUILayout.Width(45))) pos.y -= 0.03f;
                if (GUILayout.Button("+0.03", GUILayout.Width(45))) pos.y += 0.03f;
                if (GUILayout.Button("-0.1", GUILayout.Width(40))) pos.y -= 0.1f;
                if (GUILayout.Button("+0.1", GUILayout.Width(40))) pos.y += 0.1f;
                if (GUILayout.Button("-1", GUILayout.Width(30))) pos.y -= 1.0f;
                if (GUILayout.Button("+1", GUILayout.Width(30))) pos.y += 1.0f;
                GUILayout.EndHorizontal();
                pos.y = GUILayout.HorizontalSlider(pos.y, -10f, 10f);

                // Position Z
                GUILayout.BeginHorizontal();
                GUILayout.Label("Z:", GUILayout.Width(20));
                pos.z = DrawFloatField(pos.z);
                if (GUILayout.Button("-0.03", GUILayout.Width(45))) pos.z -= 0.03f;
                if (GUILayout.Button("+0.03", GUILayout.Width(45))) pos.z += 0.03f;
                if (GUILayout.Button("-0.1", GUILayout.Width(40))) pos.z -= 0.1f;
                if (GUILayout.Button("+0.1", GUILayout.Width(40))) pos.z += 0.1f;
                if (GUILayout.Button("-1", GUILayout.Width(30))) pos.z -= 1.0f;
                if (GUILayout.Button("+1", GUILayout.Width(30))) pos.z += 1.0f;
                GUILayout.EndHorizontal();
                pos.z = GUILayout.HorizontalSlider(pos.z, -10f, 10f);

                GUILayout.Space(4);
                GUILayout.Label("<b>Camera Rotation (Pitch / Yaw / Roll):</b>");

                // Pitch X
                GUILayout.BeginHorizontal();
                GUILayout.Label("Pitch (X):", GUILayout.Width(65));
                rot.x = DrawFloatField(rot.x);
                if (GUILayout.Button("-5°", GUILayout.Width(40))) rot.x -= 5f;
                if (GUILayout.Button("+5°", GUILayout.Width(40))) rot.x += 5f;
                if (GUILayout.Button("-15°", GUILayout.Width(45))) rot.x -= 15f;
                if (GUILayout.Button("+15°", GUILayout.Width(45))) rot.x += 15f;
                GUILayout.EndHorizontal();

                // Yaw Y
                GUILayout.BeginHorizontal();
                GUILayout.Label("Yaw (Y):", GUILayout.Width(65));
                rot.y = DrawFloatField(rot.y);
                if (GUILayout.Button("-5°", GUILayout.Width(40))) rot.y -= 5f;
                if (GUILayout.Button("+5°", GUILayout.Width(40))) rot.y += 5f;
                if (GUILayout.Button("-15°", GUILayout.Width(45))) rot.y -= 15f;
                if (GUILayout.Button("+15°", GUILayout.Width(45))) rot.y += 15f;
                GUILayout.EndHorizontal();

                // Roll Z
                GUILayout.BeginHorizontal();
                GUILayout.Label("Roll (Z):", GUILayout.Width(65));
                rot.z = DrawFloatField(rot.z);
                if (GUILayout.Button("-5°", GUILayout.Width(40))) rot.z -= 5f;
                if (GUILayout.Button("+5°", GUILayout.Width(40))) rot.z += 5f;
                if (GUILayout.Button("Reset", GUILayout.Width(50))) rot.z = 0f;
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.Label($"<b>Field of View (FOV):</b> {fov:F1}°");
                fov = GUILayout.HorizontalSlider(fov, 10f, 120f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("30° (Tele)")) fov = 30f;
                if (GUILayout.Button("60° (Std)")) fov = 60f;
                if (GUILayout.Button("90° (Wide)")) fov = 90f;
                if (GUILayout.Button("110° (Ultra)")) fov = 110f;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // ==========================================
            // 💾 4. CAMERA PRESETS (SAVED SLOTS)
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            int savedCount = 0;
            for (int i = 0; i < 4; i++) if (presets[i].isSaved) savedCount++;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>💾 4. Camera Presets</b> <color=#00FF99>[{savedCount}/4 Saved]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSectionPresets ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSectionPresets = !showSectionPresets;
            }
            GUILayout.EndHorizontal();

            if (showSectionPresets)
            {
                GUILayout.Space(3);
                for (int i = 0; i < 4; i++)
                {
                    int pIndex = i;
                    GUILayout.BeginHorizontal();
                    string status = presets[pIndex].isSaved ? "<color=#00FF99>Saved</color>" : "<color=#888888>Empty</color>";
                    if (GUILayout.Button($"Save Cam P{pIndex + 1}", GUILayout.Width(110)))
                    {
                        SaveCameraPresetIndex(pIndex);
                    }
                    if (GUILayout.Button($"Load Cam P{pIndex + 1} ({status})") && presets[pIndex].isSaved)
                    {
                        LoadCameraPresetIndex(pIndex);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            // ==========================================
            // 🧱 5. 3D PHYSICAL SCENE OBJECTS (CYCLORAMA & HDRI)
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            string shapeBadge = loadedShapeTexture != null ? (show3DShape ? "Active: " + loadedShapeFileName : "Hidden") : "None";
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>🧱 5. 3D Scene Objects & Cyclorama</b> <color=#00FF99>[{shapeBadge}]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSection3DShape ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSection3DShape = !showSection3DShape;
            }
            GUILayout.EndHorizontal();

            if (showSection3DShape)
            {
                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("📁 Choose File (HDR, JPG, PNG)", GUILayout.Height(28)))
                {
                    PromptSelectShapeFile();
                }
                if (GUILayout.Button("🎯 Place Behind View", GUILayout.Width(145), GUILayout.Height(28)))
                {
                    Place3DShapeInFrontOfCamera();
                }
                if (GUILayout.Button("🧹 Clear", GUILayout.Width(60), GUILayout.Height(28)))
                {
                    ClearShapeObject();
                }
                GUILayout.EndHorizontal();

                if (loadedShapeTexture != null)
                {
                    GUILayout.Space(2);
                    GUILayout.Label($"3D Object: <b>{loadedShapeFileName}</b> ({loadedShapeTexture.width}x{loadedShapeTexture.height})");

                    show3DShape = GUILayout.Toggle(show3DShape, " <b>Show 3D Shape in Scene</b>");

                    // Shape selection
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(currentShape == Shape3DType.CurvedBackdrop3D, "🖼️ Curved Backdrop"))
                    {
                        if (currentShape != Shape3DType.CurvedBackdrop3D) { currentShape = Shape3DType.CurvedBackdrop3D; shapeScale = new Vector3(25f, 15f, 25f); RebuildShapeMesh(); }
                    }
                    if (GUILayout.Toggle(currentShape == Shape3DType.Sphere360HDRI, "🌐 360° Sphere Dome"))
                    {
                        if (currentShape != Shape3DType.Sphere360HDRI) { currentShape = Shape3DType.Sphere360HDRI; shapeScale = new Vector3(300f, 300f, 300f); RebuildShapeMesh(); }
                    }
                    if (GUILayout.Toggle(currentShape == Shape3DType.FlatWall3D, "🔲 Flat Wall"))
                    {
                        if (currentShape != Shape3DType.FlatWall3D) { currentShape = Shape3DType.FlatWall3D; shapeScale = new Vector3(25f, 15f, 1f); RebuildShapeMesh(); }
                    }
                    GUILayout.EndHorizontal();

                    if (currentShape == Shape3DType.CurvedBackdrop3D)
                    {
                        // Curvature Bend Sides (Horizontal)
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Bend Sides (Wrap X): {bendSidesAngle:F0}°", GUILayout.Width(165));
                        float newBendX = GUILayout.HorizontalSlider(bendSidesAngle, 0f, 240f);
                        if (Mathf.Abs(newBendX - bendSidesAngle) > 0.5f)
                        {
                            bendSidesAngle = newBendX;
                            RebuildShapeMesh();
                        }
                        if (GUILayout.Button("0°", GUILayout.Width(30))) { bendSidesAngle = 0f; RebuildShapeMesh(); }
                        if (GUILayout.Button("90°", GUILayout.Width(35))) { bendSidesAngle = 90f; RebuildShapeMesh(); }
                        if (GUILayout.Button("180°", GUILayout.Width(40))) { bendSidesAngle = 180f; RebuildShapeMesh(); }
                        GUILayout.EndHorizontal();

                        // Curvature Bend Top/Bottom (Vertical)
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Bend Top/Bottom (Y): {bendTopBottomAngle:F0}°", GUILayout.Width(165));
                        float newBendY = GUILayout.HorizontalSlider(bendTopBottomAngle, 0f, 120f);
                        if (Mathf.Abs(newBendY - bendTopBottomAngle) > 0.5f)
                        {
                            bendTopBottomAngle = newBendY;
                            RebuildShapeMesh();
                        }
                        if (GUILayout.Button("0°", GUILayout.Width(30))) { bendTopBottomAngle = 0f; RebuildShapeMesh(); }
                        if (GUILayout.Button("45°", GUILayout.Width(35))) { bendTopBottomAngle = 45f; RebuildShapeMesh(); }
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(4);
                    GUILayout.Label("<b>3D Object Scale & Dimensions:</b>");

                    // Manual Numeric Scale Width (X)
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Width (Scale X):", GUILayout.Width(110));
                    shapeScale.x = DrawFloatField(shapeScale.x);
                    shapeScale.x = GUILayout.HorizontalSlider(shapeScale.x, 1f, 1000f);
                    if (GUILayout.Button("-5", GUILayout.Width(30))) shapeScale.x = Mathf.Max(1f, shapeScale.x - 5f);
                    if (GUILayout.Button("+5", GUILayout.Width(30))) shapeScale.x += 5f;
                    GUILayout.EndHorizontal();

                    // Manual Numeric Scale Height (Y)
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Height (Scale Y):", GUILayout.Width(110));
                    shapeScale.y = DrawFloatField(shapeScale.y);
                    shapeScale.y = GUILayout.HorizontalSlider(shapeScale.y, 1f, 1000f);
                    if (GUILayout.Button("-5", GUILayout.Width(30))) shapeScale.y = Mathf.Max(1f, shapeScale.y - 5f);
                    if (GUILayout.Button("+5", GUILayout.Width(30))) shapeScale.y += 5f;
                    GUILayout.EndHorizontal();

                    // Manual Numeric Scale Depth / Radius (Z)
                    if (currentShape == Shape3DType.Sphere360HDRI)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Radius (Scale Z):", GUILayout.Width(110));
                        shapeScale.z = DrawFloatField(shapeScale.z);
                        shapeScale.z = GUILayout.HorizontalSlider(shapeScale.z, 10f, 2000f);
                        shapeScale.x = shapeScale.z;
                        shapeScale.y = shapeScale.z;
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Depth (Scale Z):", GUILayout.Width(110));
                        shapeScale.z = DrawFloatField(shapeScale.z);
                        shapeScale.z = GUILayout.HorizontalSlider(shapeScale.z, 1f, 200f);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(4);
                    GUILayout.Label("<b>3D Object World Position & Rotation:</b>");

                    // World Position X
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Pos X:", GUILayout.Width(45));
                    shapeWorldPos.x = DrawFloatField(shapeWorldPos.x);
                    shapeWorldPos.x = GUILayout.HorizontalSlider(shapeWorldPos.x, -50f, 50f);
                    if (GUILayout.Button("0", GUILayout.Width(25))) shapeWorldPos.x = 0f;
                    GUILayout.EndHorizontal();

                    // World Position Y
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Pos Y:", GUILayout.Width(45));
                    shapeWorldPos.y = DrawFloatField(shapeWorldPos.y);
                    shapeWorldPos.y = GUILayout.HorizontalSlider(shapeWorldPos.y, -50f, 50f);
                    if (GUILayout.Button("0", GUILayout.Width(25))) shapeWorldPos.y = 0f;
                    GUILayout.EndHorizontal();

                    // World Position Z
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Pos Z:", GUILayout.Width(45));
                    shapeWorldPos.z = DrawFloatField(shapeWorldPos.z);
                    shapeWorldPos.z = GUILayout.HorizontalSlider(shapeWorldPos.z, -50f, 50f);
                    if (GUILayout.Button("0", GUILayout.Width(25))) shapeWorldPos.z = 0f;
                    GUILayout.EndHorizontal();

                    // World Rotation Yaw (Y)
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Rot Y (Yaw): {shapeWorldRot.y:F0}°", GUILayout.Width(125));
                    shapeWorldRot.y = GUILayout.HorizontalSlider(shapeWorldRot.y, 0f, 360f);
                    if (GUILayout.Button("0°", GUILayout.Width(30))) shapeWorldRot.y = 0f;
                    GUILayout.EndHorizontal();

                    // World Rotation Pitch (X)
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Rot X (Pitch): {shapeWorldRot.x:F0}°", GUILayout.Width(125));
                    shapeWorldRot.x = GUILayout.HorizontalSlider(shapeWorldRot.x, -90f, 90f);
                    if (GUILayout.Button("0°", GUILayout.Width(30))) shapeWorldRot.x = 0f;
                    GUILayout.EndHorizontal();

                    // Brightness / Exposure
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Brightness / Light: {shapeBrightness:F2}x", GUILayout.Width(140));
                    shapeBrightness = GUILayout.HorizontalSlider(shapeBrightness, 0.05f, 5.0f);
                    if (GUILayout.Button("1x", GUILayout.Width(30))) shapeBrightness = 1.0f;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            // ==========================================
            // 🎨 6. SKYBOX & SOLID BACKGROUND / ALPHA
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            string skyboxBadge = disableSkybox ? $"Solid ({bgColor.r:F1},{bgColor.g:F1},{bgColor.b:F1}, A={bgColor.a:F1})" : "Default 3D Skybox";
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>🎨 6. Skybox & Background Fill</b> <color=#00FF99>[{skyboxBadge}]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSectionSkybox ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSectionSkybox = !showSectionSkybox;
            }
            GUILayout.EndHorizontal();

            if (showSectionSkybox)
            {
                GUILayout.Space(3);
                disableSkybox = GUILayout.Toggle(disableSkybox, " <b>Disable Skybox (Solid Fill / Transparent PNG)</b>");

                if (disableSkybox)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"R: {bgColor.r:F2}", GUILayout.Width(55));
                    bgColor.r = GUILayout.HorizontalSlider(bgColor.r, 0f, 1f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"G: {bgColor.g:F2}", GUILayout.Width(55));
                    bgColor.g = GUILayout.HorizontalSlider(bgColor.g, 0f, 1f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"B: {bgColor.b:F2}", GUILayout.Width(55));
                    bgColor.b = GUILayout.HorizontalSlider(bgColor.b, 0f, 1f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Alpha: {bgColor.a:F2}", GUILayout.Width(65));
                    bgColor.a = GUILayout.HorizontalSlider(bgColor.a, 0f, 1f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Chroma Green")) bgColor = new Color(0f, 1f, 0f, 1f);
                    if (GUILayout.Button("Chroma Blue")) bgColor = new Color(0f, 0f, 1f, 1f);
                    if (GUILayout.Button("Black")) bgColor = new Color(0f, 0f, 0f, 1f);
                    if (GUILayout.Button("Transparent (A=0)")) bgColor = new Color(0f, 0f, 0f, 0f);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            // ==========================================
            // 💻 7. PROXIMA INSPECTOR & CHEATS TERMINAL
            // ==========================================
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>💻 7. Proxima In-Game Inspector & Cheats</b> <color=#00FF99>[Terminal]</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(showSectionProxima ? "▲ Hide" : "▼ Open", GUILayout.Width(75), GUILayout.Height(20)))
            {
                showSectionProxima = !showSectionProxima;
            }
            GUILayout.EndHorizontal();

            if (showSectionProxima)
            {
                GUILayout.Space(3);
                // Terminal input
                GUILayout.BeginHorizontal();
                terminalInput = GUILayout.TextField(terminalInput, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Run", GUILayout.Width(60)))
                {
                    ExecuteCustomCommand(terminalInput);
                    terminalInput = "";
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.Label("<b>Quick Game Actions:</b>");

                // SetLevel & LoadLevel & LoadMenu
                GUILayout.BeginHorizontal();
                GUILayout.Label("Lvl:", GUILayout.Width(30));
                inputLevelStr = GUILayout.TextField(inputLevelStr, GUILayout.Width(45));
                if (GUILayout.Button("Set Level", GUILayout.Width(80)))
                {
                    if (int.TryParse(inputLevelStr, out int lvl)) CallProximaMethod("SetLevel", lvl);
                }
                if (GUILayout.Button("Load Level", GUILayout.Width(85)))
                {
                    CallProximaMethod("LoadLevel");
                }
                if (GUILayout.Button("Load Menu", GUILayout.Width(85)))
                {
                    CallProximaMethod("LoadMenu");
                }
                GUILayout.EndHorizontal();

                // SetCoins
                GUILayout.BeginHorizontal();
                GUILayout.Label("Coins:", GUILayout.Width(45));
                inputCoinsStr = GUILayout.TextField(inputCoinsStr, GUILayout.Width(60));
                if (GUILayout.Button("Set Coins (set_coins)", GUILayout.ExpandWidth(true)))
                {
                    if (int.TryParse(inputCoinsStr, out int coins)) CallProximaMethod("SetCoins", coins);
                }
                GUILayout.EndHorizontal();

                // SetLives
                GUILayout.BeginHorizontal();
                GUILayout.Label("Lives:", GUILayout.Width(45));
                inputLivesStr = GUILayout.TextField(inputLivesStr, GUILayout.Width(60));
                if (GUILayout.Button("Set Lives (set_lives)", GUILayout.ExpandWidth(true)))
                {
                    if (int.TryParse(inputLivesStr, out int lives)) CallProximaMethod("SetLives", lives);
                }
                GUILayout.EndHorizontal();

                // SetStars
                GUILayout.BeginHorizontal();
                GUILayout.Label("Stars:", GUILayout.Width(45));
                inputStarsStr = GUILayout.TextField(inputStarsStr, GUILayout.Width(60));
                if (GUILayout.Button("Set Stars (set_stars)", GUILayout.ExpandWidth(true)))
                {
                    if (int.TryParse(inputStarsStr, out int stars)) CallProximaMethod("SetStars", stars);
                }
                GUILayout.EndHorizontal();

                // WinLevel / LoseLevel
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🏆 Win Level (win_level)")) CallProximaMethod("WinLevel");
                if (GUILayout.Button("💀 Lose Level (lose_level)")) CallProximaMethod("LoseLevel");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🔄 Recenter Area")) CallProximaMethod("RecenterArea");
                if (GUILayout.Button("🧹 Reset Tutorials")) CallProximaMethod("ResetTutorials");
                if (GUILayout.Button("Clear Save")) CallProximaMethod("ClearSave");
                GUILayout.EndHorizontal();

                // Terminal Log output box
                GUILayout.Space(2);
                logScrollPos = GUILayout.BeginScrollView(logScrollPos, GUILayout.Height(110));
                GUILayout.TextArea(terminalLog, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void SaveCameraPresetIndex(int pIndex)
        {
            presets[pIndex].posX = pos.x;
            presets[pIndex].posY = pos.y;
            presets[pIndex].posZ = pos.z;
            presets[pIndex].rotX = rot.x;
            presets[pIndex].rotY = rot.y;
            presets[pIndex].rotZ = rot.z;
            presets[pIndex].fov = fov;
            presets[pIndex].moveSpeed = moveSpeed;
            presets[pIndex].lockWorldPos = (currentFollowMode == FollowMode.FreeFlight_World);
            presets[pIndex].isSaved = true;

            SaveAllPersistentSettings();
            terminalLog += $"\n[PRESET] Camera Preset P{pIndex + 1} saved (Camera only).";
            ShowNotice($"💾 Camera Preset P{pIndex + 1} Salvat!");
        }

        private void LoadCameraPresetIndex(int pIndex)
        {
            if (!presets[pIndex].isSaved) return;

            pos = new Vector3(presets[pIndex].posX, presets[pIndex].posY, presets[pIndex].posZ);
            rot = new Vector3(presets[pIndex].rotX, presets[pIndex].rotY, presets[pIndex].rotZ);
            fov = presets[pIndex].fov;
            if (presets[pIndex].moveSpeed > 0) moveSpeed = presets[pIndex].moveSpeed;
            currentFollowMode = FollowMode.FreeFlight_World;

            terminalLog += $"\n[PRESET] Loaded Camera Preset P{pIndex + 1} (Skybox untouched).";
            ShowNotice($"📂 Camera Preset P{pIndex + 1} Încărcat!");
        }

        private object GetProximaInstance(Type type)
        {
            try
            {
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", new Type[] { typeof(Type), typeof(bool) });
                if (findMethod != null)
                {
                    var objs = (UnityEngine.Object[])findMethod.Invoke(null, new object[] { type, true });
                    if (objs != null && objs.Length > 0 && objs[0] != null) return objs[0];
                }
            }
            catch { }

            try
            {
                var objs = Resources.FindObjectsOfTypeAll(type);
                if (objs != null && objs.Length > 0 && objs[0] != null) return objs[0];
            }
            catch { }

            try
            {
                var asmCSharp = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                Type installerType = asmCSharp?.GetType("Infrastructure.Installers.ProximaInstaller");
                if (installerType != null)
                {
                    var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", new Type[] { typeof(Type), typeof(bool) });
                    UnityEngine.Object[] installers = null;
                    if (findMethod != null) installers = (UnityEngine.Object[])findMethod.Invoke(null, new object[] { installerType, true });
                    if (installers == null || installers.Length == 0) installers = Resources.FindObjectsOfTypeAll(installerType);

                    if (installers != null && installers.Length > 0 && installers[0] != null)
                    {
                        var field = installerType.GetField("proximaCommands", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            var val = field.GetValue(installers[0]);
                            if (val != null) return val;
                        }
                    }
                }
            }
            catch { }

            try
            {
                GameObject go = new GameObject("[ProximaCommands_Auto]");
                DontDestroyOnLoad(go);
                var created = go.AddComponent(type);
                if (created != null) return created;
            }
            catch { }

            return null;
        }

        private void CallProximaMethod(string methodName, params object[] args)
        {
            string argsStr = args != null && args.Length > 0 ? string.Join(", ", args) : "";
            terminalLog += $"\n> {methodName}({argsStr})";

            try
            {
                var asmCSharp = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                if (asmCSharp == null)
                {
                    terminalLog += "\n[ERROR] Assembly-CSharp not found.";
                    return;
                }

                Type[] proximaTypes = asmCSharp.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith("Proxima")).ToArray();
                bool methodFound = false;

                foreach (var type in proximaTypes)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        if (string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
                        {
                            var paramsInfo = m.GetParameters();
                            int reqCount = args != null ? args.Length : 0;

                            if (paramsInfo.Length == reqCount)
                            {
                                methodFound = true;
                                object instance = m.IsStatic ? null : GetProximaInstance(type);

                                if (!m.IsStatic && instance == null)
                                {
                                    terminalLog += $"\n[ERROR] Could not resolve instance for {type.Name}.";
                                    continue;
                                }

                                object[] convertedArgs = null;
                                if (reqCount > 0)
                                {
                                    convertedArgs = new object[reqCount];
                                    for (int i = 0; i < reqCount; i++)
                                    {
                                        Type pType = paramsInfo[i].ParameterType;
                                        convertedArgs[i] = Convert.ChangeType(args[i], pType);
                                    }
                                }

                                m.Invoke(instance, convertedArgs);
                                terminalLog += $"\n[SUCCESS] Executed {type.Name}.{m.Name}({argsStr})!";
                                return;
                            }
                        }
                    }
                }

                if (!methodFound)
                {
                    terminalLog += $"\n[ERROR] Method '{methodName}' with {args?.Length ?? 0} args not found in Proxima.";
                }
            }
            catch (Exception ex)
            {
                terminalLog += $"\n[EX] {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private void ExecuteCustomCommand(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            input = input.Trim();

            string[] parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0];

            if (parts.Length == 1)
            {
                CallProximaMethod(cmd);
            }
            else
            {
                object[] args = new object[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i], out int intVal)) args[i - 1] = intVal;
                    else if (float.TryParse(parts[i], out float floatVal)) args[i - 1] = floatVal;
                    else if (bool.TryParse(parts[i], out bool boolVal)) args[i - 1] = boolVal;
                    else args[i - 1] = parts[i];
                }
                CallProximaMethod(cmd, args);
            }
        }

        private float DrawFloatField(float val)
        {
            string str = val.ToString("F2");
            str = GUILayout.TextField(str, GUILayout.Width(65));
            float parsed;
            if (float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) || float.TryParse(str, out parsed)) return parsed;
            return val;
        }
    }

    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> executionQueue = new ConcurrentQueue<Action>();

        public static void Enqueue(Action action)
        {
            if (action != null) executionQueue.Enqueue(action);
        }

        public static void ProcessQueue()
        {
            while (executionQueue.TryDequeue(out Action action))
            {
                try { action?.Invoke(); } catch { }
            }
        }
    }
}
