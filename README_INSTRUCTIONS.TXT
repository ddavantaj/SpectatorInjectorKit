================================================================================
   UNITY SPECTATOR CAMERA & 3D VIRTUAL STUDIO SUITE (v3.1.2)
   INSTRUCTION MANUAL / MANUAL DE INSTRUCTIUNI / ИНСТРУКЦИЯ ПО ЭКСПЛУАТАЦИИ
================================================================================

LANGUAGES INCLUDED / LIMBI INCLUSE / ВКЛЮЧЕННЫЕ ЯЗЫКИ:
  1. ENGLISH (EN)
  2. ROMANIAN / ROMANA (RO)
  3. RUSSIAN / РУССКИЙ (RU)

================================================================================
   1. ENGLISH (EN) - USER GUIDE & DOCUMENTATION
================================================================================

[ 1.1 QUICK INSTALLATION (1-CLICK INJECTOR) ]
--------------------------------------------------------------------------------
1. Open the SpectatorInjectorKit folder.
2. Double-click "Inject.bat" (or "InjectSpectatorCamera.exe").
3. Select your game build directory when prompted (where the .exe is located).
4. The patcher automatically injects "SpectatorPlugin.dll" and bundles "ffmpeg.exe".
5. Launch your game executable. The Spectator overlay will appear automatically.

[ 1.2 KEYBOARD SHORTCUTS & CONTROLS ]
--------------------------------------------------------------------------------
- F1             : Toggle Spectator UI on / off (also cancels Clean Capture auto-hide)
- F9             : Capture Screenshot (Alpha PNG / 8x MSAA) - panel auto-hides if Clean Capture is on
- F10            : Toggle Screen Recording (Zero-Lag 60 FPS MP4 + In-Game Audio, 1:1 Real-Time)
- W / A / S / D  : Move Camera Forward / Left / Backward / Right in World Space
- E / Space      : Fly Upwards in World Space
- Q / Left Ctrl  : Fly Downwards in World Space
- Left Shift     : Hold for Fast Speed Boost (Turbo)
- Right-Click    : Hold & Drag mouse to Look Around / Orbit (Pitch & Yaw)

[ 1.3 TABBED CONTROL PANEL & CLEAN CAPTURE ]
--------------------------------------------------------------------------------
A status strip (target, tracking mode, resolution, FOV, REC timer) and pinned
capture buttons sit above 7 tabs, so only one category is drawn at a time:
1. 🎥 Video      : Resolution (1080p/720p/Native), FPS (60/30),
                    Hardware NVENC mode, Steadicam Gimbal damping, Clean Capture toggle.
2. 👤 Tracking   : Follow VR Headset (1st Person) vs Locked World, flight speed, mouse sensitivity.
3. 🎬 Camera     : Position, Rotation & FOV with self-expanding sliders and axis step buttons.
4. 💾 Presets    : 4 quick save/load slots for camera coordinates only.
5. 🧱 3D Scene   : Curved Backdrop, 360° Dome, Flat Wall, HDR loader, world transform.
6. 🎨 Backdrop   : Disable Skybox, Chroma Green/Blue, Alpha PNG cutout.
7. 💻 Console    : Interactive terminal & 1-click level/coin controls.

Clean Capture (on by default) hides the whole panel the instant you take a
screenshot or start recording, and brings it back automatically when the
capture ends - so it never appears in your screenshots or videos. The window
is resizable from its corner and remembers position, size and active tab.

[ 1.4 HIGH-PERFORMANCE ZERO-LAG VIDEO & STABILIZATION ]
--------------------------------------------------------------------------------
- Zero-GC Buffer Pool  : Pre-allocated frame recycling with 0 MB/s allocation.
- Steadicam Damping    : Smooth positional and spherical rotational interpolation.
                         Presets: Subtle (12.0), Cinematic (6.0), Heavy (2.5).
- 1:1 Real-Time Sync   : Eliminates fast-forward / time-skew effects on VR displays.

================================================================================
   2. ROMÂNĂ (RO) - GHID DE UTILIZARE ȘI INSTRUCȚIUNI
================================================================================

[ 2.1 INSTALARE RAPIDĂ (INJECTOR STANDALONE CU 1-CLICK) ]
--------------------------------------------------------------------------------
1. Deschideți folderul SpectatorInjectorKit.
2. Rulați "Inject.bat" (sau "InjectSpectatorCamera.exe").
3. Selectați folderul cu build-ul jocului (unde se află fișierul .exe al jocului).
4. Injectorul va insera automat "SpectatorPlugin.dll" și "ffmpeg.exe".
5. Porniți jocul. Panoul de control Spectator va apărea direct pe ecran.

[ 2.2 SCURTĂTURI DE TASTATURĂ ]
--------------------------------------------------------------------------------
- F1             : Ascunde / Afișează panoul de control
- F9             : Screenshot PNG ultra-clar (cu suport pentru fundal transparent)
- F10            : Pornește / Oprește înregistrarea video MP4 la 60 FPS cu Sunet Joc (Fără Lag)
- W / A / S / D  : Zbor Cameră: Înainte / Stânga / Înapoi / Dreapta
- E / Space      : Ridicare pe verticală
- Q / Left Ctrl  : Coborâre pe verticală
- Shift Stânga   : Viteză mărită de deplasare (Turbo)
- Click Dreapta  : Țineți apăsat și mișcați mouse-ul pentru rotire privire

[ 2.3 PANOU DE CONTROL PE TAB-URI & CLEAN CAPTURE ]
--------------------------------------------------------------------------------
O bară de status (țintă, mod tracking, rezoluție, FOV, timer REC) și butoanele
de captură stau fixe deasupra a 7 tab-uri, deci se randează doar unul odată:
1. 🎥 Video      : Rezoluții (1080p, 720p, Native), 60/30 FPS, mod Zero-Lag
                    NVENC, stabilizare Steadicam și comutatorul Clean Capture.
2. 👤 Tracking   : Urmărire cască VR (Persoana 1) vs Zbor Liber, viteză, sensibilitate mouse.
3. 🎬 Camera     : Poziție, Rotație, Unghi FOV cu slider-e care se extind automat.
4. 💾 Presets    : 4 preseturi independente pentru poziția camerei.
5. 🧱 3D Scene   : Fundal Curbat 3D, Dom 360°, Perete Plat, Încărcare fișiere HDR.
6. 🎨 Backdrop   : Oprire Skybox, Fundal Solid Chroma Green/Blue/Alpha.
7. 💻 Console    : Consolă comenzi, Setare Nivel, Bani, Vieți, Stele.

Clean Capture (activ implicit) ascunde tot panoul instant când faci un
screenshot sau pornești o înregistrare, și îl aduce automat înapoi la final -
astfel nu mai apare niciodată în poze sau video. Fereastra se redimensionează
din colț și reține poziția, dimensiunea și tab-ul activ.

[ 2.4 ÎNREGISTRARE VIDEO ZERO-LAG & STABILIZARE STEADICAM ]
--------------------------------------------------------------------------------
- Zero-GC Engine       : Reutilizare automată de buffere de memorie (0 MB alocare GC).
- Stabilizare Gimbal   : Mișcare cinematică fluidă prin interpolare sferică continuă.
- Sincronizare 1:1     : Timpul din video corespunde milisecundă cu milisecundă cu jocul.

================================================================================
   3. РУССКИЙ (RU) - РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ
================================================================================

[ 3.1 БЫСТРАЯ УСТАНОВКА (1-CLICK ИНЖЕКТОР) ]
--------------------------------------------------------------------------------
1. Откройте папку SpectatorInjectorKit.
2. Запустите "Inject.bat" (или "InjectSpectatorCamera.exe").
3. Укажите папку с установленной игрой (где находится .exe файл).
4. Инжектор автоматически установит "SpectatorPlugin.dll" и "ffmpeg.exe".
5. Запустите игру. Панель управления Spectator появится на экране.

[ 3.2 ГОРЯЧИЕ КЛАВИШИ ]
--------------------------------------------------------------------------------
- F1             : Скрыть / Показать панель управления
- F9             : Сделать скриншот PNG (с поддержкой прозрачности Alpha)
- F10            : Старт / Стоп записи видео MP4 60 FPS со звуком игры (Без лагов)
- W / A / S / D  : Перемещение камеры в 3D пространстве
- E / Пробел     : Подняться вверх
- Q / Left Ctrl  : Опуститься вниз
- Left Shift     : Ускорение полета (Турбо)
- Правая кнопка  : Удерживайте для вращения и обзора мышью

[ 3.3 ПАНЕЛЬ УПРАВЛЕНИЯ НА ВКЛАДКАХ & CLEAN CAPTURE ]
--------------------------------------------------------------------------------
Строка статуса (цель, режим слежения, разрешение, FOV, таймер REC) и кнопки
захвата закреплены над 7 вкладками, отображается только одна за раз:
1. 🎥 Video      : Настройки 1080p/720p, 60 FPS, NVENC, Steadicam, переключатель Clean Capture.
2. 👤 Tracking   : Слежение за VR шлемом (1-е лицо) / Свободный полет, скорость, чувствительность мыши.
3. 🎬 Camera     : Координаты X/Y/Z, Угол обзора FOV с саморасширяющимися ползунками.
4. 💾 Presets    : 4 слота быстрых пресетов камеры.
5. 🧱 3D Scene   : 3D изогнутый фон, купол 360°, плоская стена, загрузка HDR файлов.
6. 🎨 Backdrop   : Отключение скайбокса, Хромакей Green/Blue/Alpha.
7. 💻 Console    : Консоль читов, Выбор уровня, Монеты, Жизни, Звезды.

Clean Capture (включен по умолчанию) мгновенно скрывает всю панель при
скриншоте или начале записи и возвращает её автоматически по завершении -
поэтому она никогда не попадает в кадр. Окно можно изменять по углу, оно
запоминает позицию, размер и активную вкладку.
================================================================================
