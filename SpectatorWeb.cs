using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SpectatorTools
{
    public partial class SpectatorCameraController
    {
        private const int WebPort = 47830;
        private TcpListener webListener;
        private Thread webThread;
        private volatile bool webRunning;
        private string webToken;
        private string webHtml;
        private volatile string webStateJson = "{}";
        private volatile string webCommandsJson = "{\"commands\":[]}";
        private float nextWebSnapshot;
        private int webCatalogRevision = -1;
        private readonly ConcurrentQueue<WebJob> webJobs = new ConcurrentQueue<WebJob>();
        private readonly SemaphoreSlim webSlots = new SemaphoreSlim(16);
        private readonly List<string> recentImages = new List<string>();
        private bool recentImagesLoaded;

        [Serializable] private sealed class WebRequest { public string action, field, value, name; public string[] args; public int slot; }
        [Serializable] private sealed class WebState
        {
            public bool connected, recording, follow, stabilization, audio, cleanCapture, shapeVisible, followWall, solidBackground, panelVisible;
            public string target, shapeFile, shapeMode, resolution, quality, log;
            public int fps, layerMask, sourceWidth, sourceHeight;
            public float fov, speed, sensitivity, damping, px, py, pz, rx, ry, rz;
            public float sx, sy, sz, ox, oy, oz, ax, ay, az, bendX, bendY, brightness, red, green, blue, alpha;
            public bool[] presets;
            public string[] recentImages;
        }
        [Serializable] private sealed class RecentImageList { public string[] paths; }
        private sealed class WebJob
        {
            public string body, response;
            public readonly ManualResetEventSlim done = new ManualResetEventSlim(false);
            // 0 waiting, 1 running, 2 cancelled: a timed-out queued command must never run later.
            public int status;
            public void Finish(bool ok, string message) { response = ResultJson(ok, message); done.Set(); }
        }

        private void StartWebPanel()
        {
            try
            {
                EnsureRecentImagesLoaded();
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpectatorPlugin.WebPanel.html"))
                {
                    if (stream == null) throw new InvalidOperationException("WebPanel.html lipseste din plugin.");
                    using (var reader = new StreamReader(stream, Encoding.UTF8)) webHtml = reader.ReadToEnd();
                }
                webToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                webListener = new TcpListener(IPAddress.Loopback, WebPort);
                webListener.Start();
                webRunning = true;
                webThread = new Thread(WebAcceptLoop) { IsBackground = true, Name = "Spectator local web" };
                webThread.Start();
                Debug.Log("[Spectator Web] Pornit: http://127.0.0.1:" + WebPort + "/");
            }
            catch (Exception ex) { Debug.LogWarning("[Spectator Web] Nu poate porni: " + ex.Message); }
        }

        private void StopWebPanel()
        {
            webRunning = false;
            try { webListener?.Stop(); } catch { }
            while (webJobs.TryDequeue(out WebJob job)) { Interlocked.Exchange(ref job.status, 2); job.Finish(false, "Jocul se inchide."); }
        }

        private void PumpWebPanel()
        {
            if (!webRunning) return;
            int processed = 0;
            while (processed++ < 16 && webJobs.TryDequeue(out WebJob job))
            {
                if (Interlocked.CompareExchange(ref job.status, 1, 0) != 0) continue;
                try { HandleWebAction(JsonUtility.FromJson<WebRequest>(job.body), job); }
                catch (Exception ex) { job.Finish(false, ex.Message); }
            }
            if (Time.unscaledTime < nextWebSnapshot) return;
            nextWebSnapshot = Time.unscaledTime + 0.4f;
            var s = new WebState {
                connected = targetCamera != null, recording = isRecording, follow = !lockWorldPosition, stabilization = enableStabilization,
                audio = recordAudio, cleanCapture = hideUIDuringCapture, shapeVisible = show3DShape, followWall = followFlatWallCamera,
                solidBackground = disableSkybox, panelVisible = showUI, target = targetName ?? "", shapeFile = loadedShapeFileName ?? "",
                shapeMode = currentShape.ToString(), resolution = videoResMode.ToString(), quality = videoQuality.ToString(), log = terminalLog ?? "",
                fps = targetRecordFPS, layerMask = targetCamera != null ? targetCamera.cullingMask : 0, sourceWidth = Screen.width, sourceHeight = Screen.height,
                fov = fov, speed = moveSpeed, sensitivity = lookSensitivity, damping = stabilizationSmoothness,
                px = pos.x, py = pos.y, pz = pos.z, rx = rot.x, ry = rot.y, rz = rot.z,
                sx = shapeScale.x, sy = shapeScale.y, sz = shapeScale.z, ox = shapeWorldPos.x, oy = shapeWorldPos.y, oz = shapeWorldPos.z,
                ax = shapeWorldRot.x, ay = shapeWorldRot.y, az = shapeWorldRot.z, bendX = bendSidesAngle, bendY = bendTopBottomAngle,
                brightness = shapeBrightness, red = bgColor.r, green = bgColor.g, blue = bgColor.b, alpha = bgColor.a,
                presets = presets.Select(p => p.isSaved).ToArray(), recentImages = recentImages.Where(File.Exists).ToArray()
            };
            webStateJson = JsonUtility.ToJson(s);
            if (webCatalogRevision != commandRevision)
            {
                webCatalogRevision = commandRevision;
                webCommandsJson = BuildWebCommands();
            }
        }

        private void WebAcceptLoop()
        {
            while (webRunning)
            {
                try
                {
                    TcpClient client = webListener.AcceptTcpClient();
                    if (!webSlots.Wait(0)) { client.Close(); continue; }
                    ThreadPool.QueueUserWorkItem(_ => { try { ServeWebClient(client); } finally { client.Close(); webSlots.Release(); } });
                }
                catch (SocketException) { if (webRunning) Thread.Sleep(100); }
                catch (ObjectDisposedException) { break; }
            }
        }

        private void ServeWebClient(TcpClient client)
        {
            try
            {
                client.ReceiveTimeout = 5000; client.SendTimeout = 5000;
                using (NetworkStream stream = client.GetStream())
                {
                    var header = new MemoryStream(); int last = 0, current;
                    while (header.Length < 16384 && (current = stream.ReadByte()) >= 0)
                    {
                        header.WriteByte((byte)current); last = (last << 8) | current;
                        if (last == 0x0d0a0d0a) break;
                    }
                    if (last != 0x0d0a0d0a) { WriteHttp(stream, 400, "text/plain", "Cerere invalidă"); return; }
                    string[] lines = Encoding.ASCII.GetString(header.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string[] first = lines[0].Split(' '); if (first.Length < 2) { WriteHttp(stream, 400, "text/plain", "Cerere invalidă"); return; }
                    string host = "", token = "", origin = "", contentType = ""; int length = 0;
                    foreach (string line in lines.Skip(1))
                    {
                        int colon = line.IndexOf(':'); if (colon < 0) continue;
                        string k = line.Substring(0, colon).Trim().ToLowerInvariant(), v = line.Substring(colon + 1).Trim();
                        if (k == "host") host = v; else if (k == "x-spectator-token") token = v; else if (k == "origin") origin = v;
                        else if (k == "content-type") contentType = v; else if (k == "content-length" && !int.TryParse(v, out length)) length = -1;
                    }
                    if (!string.Equals(host, "127.0.0.1:" + WebPort, StringComparison.OrdinalIgnoreCase)) { WriteHttp(stream, 403, "text/plain", "Interzis"); return; }
                    string method = first[0], path = first[1].Split('?')[0];
                    if (method == "GET" && path == "/") { WriteHttp(stream, 200, "text/html; charset=utf-8", webHtml.Replace("__SPECTATOR_TOKEN__", webToken)); return; }
                    if (!string.Equals(token, webToken, StringComparison.Ordinal)) { WriteHttp(stream, 403, "text/plain", "Interzis"); return; }
                    if (method == "GET" && path == "/api/state") { WriteHttp(stream, 200, "application/json; charset=utf-8", webStateJson); return; }
                    if (method == "GET" && path == "/api/commands") { WriteHttp(stream, 200, "application/json; charset=utf-8", webCommandsJson); return; }
                    if (method != "POST" || path != "/api/action") { WriteHttp(stream, 404, "text/plain", "Negăsit"); return; }
                    if (origin != "http://127.0.0.1:" + WebPort || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) { WriteHttp(stream, 403, "text/plain", "Interzis"); return; }
                    if (length < 1 || length > 32768) { WriteHttp(stream, 400, "text/plain", "Cerere prea mare"); return; }
                    byte[] body = new byte[length]; int read = 0;
                    while (read < length) { int n = stream.Read(body, read, length - read); if (n <= 0) return; read += n; }
                    var job = new WebJob { body = Encoding.UTF8.GetString(body) }; webJobs.Enqueue(job);
                    if (!job.done.Wait(10000))
                    {
                        Interlocked.CompareExchange(ref job.status, 2, 0);
                        WriteHttp(stream, 503, "application/json; charset=utf-8", ResultJson(false, "Jocul nu a răspuns la timp.")); return;
                    }
                    WriteHttp(stream, 200, "application/json; charset=utf-8", job.response);
                }
            }
            catch (Exception) { }
        }

        private static void WriteHttp(Stream stream, int code, string type, string text)
        {
            byte[] body = Encoding.UTF8.GetBytes(text ?? "");
            string reason = code == 200 ? "OK" : code == 403 ? "Forbidden" : code == 404 ? "Not Found" : code == 503 ? "Service Unavailable" : "Bad Request";
            byte[] headers = Encoding.ASCII.GetBytes("HTTP/1.1 " + code + " " + reason + "\r\nContent-Type: " + type + "\r\nContent-Length: " + body.Length + "\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; base-uri 'none'; frame-ancestors 'none'\r\nConnection: close\r\n\r\n");
            stream.Write(headers, 0, headers.Length); stream.Write(body, 0, body.Length);
        }
        private static string ResultJson(bool ok, string message) => "{\"ok\":" + (ok ? "true" : "false") + ",\"message\":\"" + EscapeJson(message ?? "") + "\"}";
        private string BuildWebCommands()
        {
            var b = new StringBuilder("{\"commands\":[");
            for (int i = 0; i < commandCatalog.Count; i++)
            {
                if (i > 0) b.Append(',');
                var c = commandCatalog[i];
                b.Append("{\"name\":\"").Append(EscapeJson(c.name ?? ""))
                    .Append("\",\"displayName\":\"").Append(EscapeJson(c.DisplayNameOrName ?? ""))
                    .Append("\",\"alias\":\"").Append(EscapeJson(c.alias ?? ""))
                    .Append("\",\"category\":\"").Append(EscapeJson(c.category ?? ""))
                    .Append("\",\"description\":\"").Append(EscapeJson(c.description ?? ""))
                    .Append("\",\"exampleInput\":\"").Append(EscapeJson(c.exampleInput ?? ""))
                    .Append("\",\"isRunnable\":").Append(c.isRunnable ? "true" : "false")
                    .Append(",\"unsupportedReason\":\"").Append(EscapeJson(c.unsupportedReason ?? ""))
                    .Append("\",\"parameters\":[");
                for (int j = 0; j < c.parameters.Length; j++)
                {
                    if (j > 0) b.Append(','); var p = c.parameters[j];
                    b.Append("{\"name\":\"").Append(EscapeJson(p.name ?? ""))
                        .Append("\",\"typeName\":\"").Append(EscapeJson(p.typeName ?? ""))
                        .Append("\",\"kind\":\"").Append(EscapeJson(p.kind ?? ""))
                        .Append("\",\"defaultText\":\"").Append(EscapeJson(p.defaultText ?? ""))
                        .Append("\",\"isRequired\":").Append(p.isRequired ? "true" : "false").Append(",\"choices\":[");
                    for (int k = 0; k < p.choices.Length; k++) { if (k > 0) b.Append(','); b.Append('"').Append(EscapeJson(p.choices[k] ?? "")).Append('"'); }
                    b.Append("]}");
                }
                b.Append("]}");
            }
            return b.Append("]}").ToString();
        }
        private static string EscapeJson(string s)
        {
            var b = new StringBuilder(); foreach (char c in s) { if (c == '"' || c == '\\') { b.Append('\\').Append(c); } else if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4")); else b.Append(c); } return b.ToString();
        }

        private void HandleWebAction(WebRequest r, WebJob job)
        {
            if (r == null || string.IsNullOrEmpty(r.action)) { job.Finish(false, "Cerere invalidă."); return; }
            if (r.action == "command")
            {
                if (commandBackend == null) { job.Finish(false, "Proxima nu este disponibilă."); return; }
                if (commandBackend.IsBusy) { job.Finish(false, "Jocul încarcă o scenă."); return; }
                commandBackend.Run(r.name, r.args ?? new string[0], (ok, text) => job.Finish(ok, text)); return;
            }
            switch (r.action)
            {
                case "set": SetWebField(r.field, r.value); break;
                case "screenshot": TakeScreenshotPNG(); break;
                case "record":
                    bool wasRecording = isRecording;
                    ToggleScreenRecording();
                    if (!wasRecording && !isRecording) throw new InvalidOperationException("Înregistrarea nu a putut porni. Verifică jurnalul jocului.");
                    if (wasRecording && (string.IsNullOrEmpty(currentVideoPath) || !File.Exists(currentVideoPath) || new FileInfo(currentVideoPath).Length == 0))
                        throw new InvalidOperationException("Înregistrarea s-a oprit, dar fișierul video nu a fost creat.");
                    break;
                case "preset_save": CheckWebSlot(r.slot); SaveCameraPresetIndex(r.slot); break;
                case "preset_load": CheckWebSlot(r.slot); if (!presets[r.slot].isSaved) throw new InvalidOperationException("Presetul este gol."); LoadCameraPresetIndex(r.slot); break;
                case "choose_image": PromptSelectShapeFile(); break;
                case "load_recent":
                    EnsureRecentImagesLoaded();
                    if (string.IsNullOrEmpty(r.value) || !recentImages.Contains(r.value) || !File.Exists(r.value)) throw new ArgumentException("Imaginea nu mai este disponibilă.");
                    LoadShapeFile(r.value);
                    if (!string.Equals(loadedShapeFilePath, r.value, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Imaginea nu a putut fi încărcată.");
                    break;
                case "clear_image": ClearShapeObject(); break;
                case "place_shape": Place3DShapeInFrontOfCamera(); break;
                case "rebind": FindTargetCamera(); break;
                case "save": SaveAllPersistentSettings(); break;
                case "open_captures": OpenCapturesDirectory(); break;
                case "refresh_commands": refreshCommands = true; break;
                case "reset_camera": pos = Vector3.zero; rot = Vector3.zero; lockWorldPosition = true; break;
                default: throw new ArgumentException("Acțiune necunoscută.");
            }
            nextWebSnapshot = 0; job.Finish(true, "OK");
        }
        private static void CheckWebSlot(int slot) { if (slot < 0 || slot > 3) throw new ArgumentException("Preset invalid."); }
        private static float WebNumber(string text, float min, float max)
        {
            if (!float.TryParse((text ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || float.IsNaN(v) || float.IsInfinity(v) || v < min || v > max)
                throw new ArgumentException("Valoare invalidă (interval " + min + "–" + max + ").");
            return v;
        }
        private static bool WebBool(string value) { if (value == "true") return true; if (value == "false") return false; throw new ArgumentException("Valoare invalidă."); }
        private void SetWebField(string field, string value)
        {
            switch (field)
            {
                case "follow": bool follow = WebBool(value); if (!follow && !lockWorldPosition && targetTransform != null) { pos = targetTransform.position; rot = targetTransform.eulerAngles; } lockWorldPosition = !follow; break;
                case "fov": fov = WebNumber(value, 10, 120); break;
                case "speed": moveSpeed = WebNumber(value, 0.1f, 10); break;
                case "sensitivity": lookSensitivity = WebNumber(value, 0.5f, 10); break;
                case "damping": stabilizationSmoothness = WebNumber(value, 1.5f, 20); break;
                case "stabilization": enableStabilization = WebBool(value); break;
                case "audio": if (isRecording) throw new InvalidOperationException("Oprește înregistrarea înainte de a schimba sunetul."); recordAudio = WebBool(value); break;
                case "cleanCapture": hideUIDuringCapture = WebBool(value); break;
                case "panelVisible": showUI = WebBool(value); break;
                case "px": pos.x = WebNumber(value, -10000, 10000); lockWorldPosition = true; break;
                case "py": pos.y = WebNumber(value, -10000, 10000); lockWorldPosition = true; break;
                case "pz": pos.z = WebNumber(value, -10000, 10000); lockWorldPosition = true; break;
                case "rx": rot.x = WebNumber(value, -3600, 3600); lockWorldPosition = true; break;
                case "ry": rot.y = WebNumber(value, -3600, 3600); lockWorldPosition = true; break;
                case "rz": rot.z = WebNumber(value, -3600, 3600); lockWorldPosition = true; break;
                case "resolution": if (isRecording) throw new InvalidOperationException("Oprește înregistrarea înainte de a schimba rezoluția."); if (!Enum.TryParse(value, out VideoResMode res) || !Enum.IsDefined(typeof(VideoResMode), res)) throw new ArgumentException("Rezoluție invalidă."); videoResMode = res; break;
                case "quality": if (isRecording) throw new InvalidOperationException("Oprește înregistrarea înainte de a schimba calitatea."); if (!Enum.TryParse(value, out VideoQualityMode q) || !Enum.IsDefined(typeof(VideoQualityMode), q)) throw new ArgumentException("Calitate invalidă."); videoQuality = q; break;
                case "fps": if (isRecording) throw new InvalidOperationException("Oprește înregistrarea înainte de a schimba FPS."); if (value != "30" && value != "60") throw new ArgumentException("FPS invalid."); targetRecordFPS = int.Parse(value); break;
                case "shapeMode": if (!Enum.TryParse(value, out Shape3DType shape) || !Enum.IsDefined(typeof(Shape3DType), shape)) throw new ArgumentException("Formă invalidă."); currentShape = shape; RebuildShapeMesh(); break;
                case "shapeVisible": show3DShape = WebBool(value); break;
                case "followWall": followFlatWallCamera = WebBool(value); if (followFlatWallCamera) Place3DShapeInFrontOfCamera(); break;
                case "sx": shapeScale.x = WebNumber(value, 1, 2000); break;
                case "sy": shapeScale.y = WebNumber(value, 1, 2000); break;
                case "sz": shapeScale.z = WebNumber(value, 1, 2000); if (currentShape == Shape3DType.Sphere360HDRI) shapeScale.x = shapeScale.y = shapeScale.z; break;
                case "ox": shapeWorldPos.x = WebNumber(value, -10000, 10000); break;
                case "oy": shapeWorldPos.y = WebNumber(value, -10000, 10000); break;
                case "oz": shapeWorldPos.z = WebNumber(value, -10000, 10000); break;
                case "ax": shapeWorldRot.x = WebNumber(value, -3600, 3600); break;
                case "ay": shapeWorldRot.y = WebNumber(value, -3600, 3600); break;
                case "az": shapeWorldRot.z = WebNumber(value, -3600, 3600); break;
                case "bendX": bendSidesAngle = WebNumber(value, 0, 240); RebuildShapeMesh(); break;
                case "bendY": bendTopBottomAngle = WebNumber(value, 0, 120); RebuildShapeMesh(); break;
                case "brightness": shapeBrightness = WebNumber(value, 0.05f, 5); break;
                case "solidBackground": disableSkybox = WebBool(value); break;
                case "red": bgColor.r = WebNumber(value, 0, 1); break;
                case "green": bgColor.g = WebNumber(value, 0, 1); break;
                case "blue": bgColor.b = WebNumber(value, 0, 1); break;
                case "alpha": bgColor.a = WebNumber(value, 0, 1); break;
                default: throw new ArgumentException("Setare necunoscută.");
            }
        }

        private string ResolutionLabel()
        {
            switch (videoResMode)
            {
                case VideoResMode.HD_720p: return "720p";
                case VideoResMode.FullHD_1080p: return "1080p";
                case VideoResMode.QHD_1440p: return "2K";
                case VideoResMode.UHD_2160p: return "4K";
                default: return "Native";
            }
        }

        private string RecentImagesPath() => Path.Combine(GetCapturesRoot(), "SpectatorRecentImages.json");
        private void EnsureRecentImagesLoaded()
        {
            if (recentImagesLoaded) return;
            recentImagesLoaded = true;
            try
            {
                string path = RecentImagesPath();
                if (!File.Exists(path)) return;
                var saved = JsonUtility.FromJson<RecentImageList>(File.ReadAllText(path));
                if (saved?.paths == null) return;
                foreach (string item in saved.paths)
                    if (!string.IsNullOrWhiteSpace(item) && !recentImages.Contains(item) && recentImages.Count < 20) recentImages.Add(item);
            }
            catch (Exception ex) { Debug.LogWarning("[Spectator Web] Imaginile recente nu au putut fi citite: " + ex.Message); }
        }
        private void RememberRecentImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            EnsureRecentImagesLoaded();
            recentImages.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            recentImages.Insert(0, path);
            if (recentImages.Count > 20) recentImages.RemoveRange(20, recentImages.Count - 20);
            try { File.WriteAllText(RecentImagesPath(), JsonUtility.ToJson(new RecentImageList { paths = recentImages.ToArray() }, true)); }
            catch (Exception ex) { Debug.LogWarning("[Spectator Web] Imaginile recente nu au putut fi salvate: " + ex.Message); }
            nextWebSnapshot = 0;
        }
    }
}
