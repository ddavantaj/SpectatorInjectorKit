================================================================================
   UNITY MATCH-3 MR SPECTATOR CAMERA & 3D CAPTURE SUITE (v2.8.0)
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
2. Double-click "InjectSpectatorCamera.bat" (or "InjectSpectatorCamera.exe").
3. Select your game build directory when prompted (where the .exe is located).
4. The patcher automatically injects "SpectatorPlugin.dll" and bundles "ffmpeg.exe".
5. Launch your game executable. The Spectator overlay will appear automatically.

[ 1.2 KEYBOARD SHORTCUTS & CONTROLS ]
--------------------------------------------------------------------------------
- F1             : Toggle Spectator UI on / off
- F9             : Capture Screenshot (Alpha PNG / 8x MSAA)
- F10            : Toggle Screen Recording (High Quality 60 FPS MP4, 1:1 Real-Time)
- W / A / S / D  : Move Camera Forward / Left / Backward / Right in World Space
- E / Space      : Fly Upwards in World Space
- Q / Left Ctrl  : Fly Downwards in World Space
- Left Shift     : Hold for Fast Speed Boost (3.5x)
- Right-Click    : Hold & Drag mouse to Look Around / Orbit (Pitch & Yaw)

[ 1.3 3D PHYSICAL IN-GAME OBJECTS & CYCLORAMA (TRUE 3D PARALLAX) ]
--------------------------------------------------------------------------------
The background is an actual 3D physical object anchored in the game scene,
providing true 3D perspective and depth parallax when the camera orbits:
- [ 🖼️ Curved Backdrop ] : 3D Curved Studio Set (Cyclorama).
  * Bend Sides (Wrap X)  : Curve sides horizontally (0° Flat -> 110° Studio Cyc -> 240° Wrap).
  * Bend Top/Bottom (Y)  : Curve top & bottom vertically (0° Flat -> 120° Bowl/Dome).
- [ 🌐 360° Sphere Dome ]: Inverted 360° sphere for HDR / panoramic environments.
- [ 🔲 Flat Wall ]       : Classic 3D flat studio wall / poster.
- Native C# Radiance HDR : Binary .hdr (RGBE) loader with Reinhard tonemapping.
                           Also supports PNG, JPG, and BMP formats.
- Manual Numeric Inputs  : Directly type exact dimensions for Width (Scale X),
                           Height (Scale Y), and Depth / Radius (Scale Z).
- [ 🎯 Place Behind View]: Snaps the 3D shape 8m in front of current camera view
                           and keeps it fixed in 3D world space.
- Brightness / Light     : Adjust light exposure from 0.05x to 5.0x.

[ 1.4 SCREENSHOTS & 1:1 REAL-TIME MP4 VIDEO RECORDING ]
--------------------------------------------------------------------------------
- F9 Screenshot (PNG)   : 8x MSAA edge anti-aliasing. Supports full transparent
                          cutouts (Disable Skybox + Alpha 0).
- F10 Video Recording   : Captures the exact Windows monitor backbuffer with all
                          shaders, post-processing, and UI.
- 1:1 Real-Time Sync    : Exact elapsed time synchronization guarantees 1s game = 1s video,
                          completely eliminating fast-forward issues on VR headsets.
- Asynchronous Engine   : Multi-threaded background streaming to FFmpeg with GPU
                          NVENC H.264 acceleration (CPU libx264 fallback).

[ 1.5 CAMERA-ONLY PRESETS (P1 - P4) ]
--------------------------------------------------------------------------------
- Save Cam P1 - P4 / Load Cam P1 - P4:
  * Saves only Camera Transform (Position, Rotation, FOV, Move Speed, World Lock).
  * Does NOT reset or alter your 3D backdrop, HDRI image, or curvature settings.

[ 1.6 FULL PERSISTENCE ACROSS GAME RESTARTS (SpectatorSettings.ini) ]
--------------------------------------------------------------------------------
- All settings automatically save every 5s, on exit, or via [ 💾 Save Now ].
- Upon reopening the game, EVERYTHING is automatically restored:
  * Camera position, rotation, FOV, and speed.
  * Active 3D shape image/HDRI file and position.
  * Width, Height, Depth scale, and Curvature Angles (Bend Sides & Top/Bottom).
  * Saved Camera Presets 1-4.
  * Proxima quick inputs (Level, Coins, Lives, Stars).

[ 1.7 PROXIMA DEBUG TERMINAL & IN-GAME CHEATS ]
--------------------------------------------------------------------------------
- One-click buttons for: Set Level, Load Level, Load Menu, Set Coins, Set Lives,
  Set Stars, Win Level, Lose Level, Recenter Area, Reset Tutorials, Clear Save.
- Custom Terminal: Execute any internal game/Proxima method directly.


================================================================================
   2. ROMANIAN / ROMANA (RO) - MANUAL DE UTILIZARE
================================================================================

[ 2.1 INSTALARE RAPIDA (INJECTOR CU 1-CLICK) ]
--------------------------------------------------------------------------------
1. Deschideti folderul SpectatorInjectorKit.
2. Dati dublu-click pe "InjectSpectatorCamera.bat" (sau "InjectSpectatorCamera.exe").
3. Selectati folderul jocului cand vi se cere (unde se afla fisierul .exe al jocului).
4. Injectorul copiaza automat "SpectatorPlugin.dll" si include "ffmpeg.exe".
5. Porniti jocul. Meniul Spectator va aparea automat pe ecran.

[ 2.2 TASTE RAPIDE & CONTROALE ]
--------------------------------------------------------------------------------
- F1             : Afiseaza / Ascunde Panoul Spectator
- F9             : Face Screenshot (Alpha PNG / 8x MSAA)
- F10            : Porneste / Opreste Inregistrarea Video MP4 (60 FPS, Viteza Reala 1:1)
- W / A / S / D  : Deplasare Inainte / Stanga / Inapoi / Dreapta in Spatiul 3D
- E / Space      : Ridicare pe verticala in sus
- Q / Left Ctrl  : Coborare pe verticala in jos
- Left Shift     : Tineti apasat pentru Viteza Rapida (Boost 3.5x)
- Click Dreapta  : Tineti apasat si miscati mouse-ul pentru Rotire Camera (Privire 3D)

[ 2.3 FORME 3D IN SCENA & STUDIO CURBAT CYCLORAMA (PARALLAX 3D REAL) ]
--------------------------------------------------------------------------------
Fundalul este un obiect 3D fizic real plasat in lumea jocului, creand adancime
si efect autentic de Parallax 3D cand rotiti camera in jurul tablei de joc:
- [ 🖼️ Curved Backdrop ] : Perete 3D Curbat de Studio (Cyclorama).
  * Bend Sides (Wrap X)  : Curbeaza lateralele (0° Plat -> 110° Studio -> 240° Imbracare).
  * Bend Top/Bottom (Y)  : Curbeaza sus si jos (0° Plat -> 120° Cupola/Bol 3D).
- [ 🌐 360° Sphere Dome ]: Sfera 3D inversata pentru medii panoramice HDRI 360°.
- [ 🔲 Flat Wall ]       : Panou 3D plat clasic pentru afise / perete de studio.
- Parser Nativ Radiance  : Citire directa binara pentru fisiere .hdr (RGBE) cu Reinhard
                           tonemapping. Suporta de asemenea PNG, JPG si BMP.
- Campuri Numerice Manual: Puteti tasta direct cifre exacte pentru Latime (Scale X),
                           Inaltime (Scale Y) si Adancime / Raza (Scale Z).
- [ 🎯 Place Behind View]: Pozitioneaza instant forma 3D la 8m in fata vederii
                           curente si o lasa fixata in coordonatele 3D ale lumii.
- Luminozitate (Light)   : Reglare expunere si intensitate de la 0.05x la 5.0x.

[ 2.4 SCREENSHOTS & INREGISTRARE VIDEO MP4 1:1 IN TIMP REAL ]
--------------------------------------------------------------------------------
- Screenshot F9 (PNG)   : Netezire margini cu 8x MSAA. Suporta decupare transparenta
                          (Disable Skybox + Alpha 0).
- Inregistrare F10 (MP4): Captureaza ecranul exact din Windows cu toate shaderele,
                          efectele de post-procesare si interfata jocului.
- Sincronizare 1:1 Reala: Sincronizare exacta cu timpul real (1s joc = 1s video),
                          eliminand accelerarea video de pe castile VR.
- Motor Asincron        : Transmisie asincrona spre FFmpeg cu accelerare hardware GPU
                          NVENC H.264 (cu fallback automat pe procesor libx264).

[ 2.5 PRESETURI DOAR PENTRU CAMERA (P1 - P4) ]
--------------------------------------------------------------------------------
- Save Cam P1 - P4 / Load Cam P1 - P4:
  * Salveaza si incarca strict parametrii Camerei (Pozitie, Rotatie, FOV, Viteza).
  * NU reseteaza si NU modifica fundalul 3D, imaginea HDRI sau curbura aleasa.

[ 2.6 SALVARE PERSISTENTA TOTALA (SpectatorSettings.ini) ]
--------------------------------------------------------------------------------
- Toate setarile se salveaza automat la fiecare 5 secunde, la inchiderea jocului
  sau prin butonul [ 💾 Save Now ].
- La redeschiderea jocului, TOTUL se restaureaza automat exact cum a ramas:
  * Pozitia, rotatia, FOV-ul si viteza camerei.
  * Imaginea 3D / HDRI incarcata si pozitia ei in spatiu.
  * Dimensiunile (Width/Height/Depth) si unghiurile de curbare (Bend Sides & Top/Bottom).
  * Preset-urile de camera 1-4.
  * Valorile tastate in Proxima (Nivel, Bani, Vieti, Stele).

[ 2.7 CONSOLA PROXIMA & TRISURI IN JOC ]
--------------------------------------------------------------------------------
- Butoane rapide: Set Level, Load Level, Load Menu, Set Coins, Set Lives,
  Set Stars, Win Level, Lose Level, Recenter Area, Reset Tutorials, Clear Save.
- Consola Text: Rulati orice comanda sau metoda interna direct din joc.


================================================================================
   3. RUSSIAN / РУССКИЙ (RU) - РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ
================================================================================

[ 3.1 БЫСТРАЯ УСТАНОВКА (ИНЖЕКТОР В 1 КЛИК) ]
--------------------------------------------------------------------------------
1. Откройте папку SpectatorInjectorKit.
2. Запустите файл "InjectSpectatorCamera.bat" (или "InjectSpectatorCamera.exe").
3. При запросе укажите папку с собранной игрой (где находится исполняемый файл .exe).
4. Инжектор автоматически установит "SpectatorPlugin.dll" и подключит "ffmpeg.exe".
5. Запустите игру. Панель камеры появится автоматически.

[ 3.2 ГОРЯЧИЕ КЛАВИШИ И УПРАВЛЕНИЕ ]
--------------------------------------------------------------------------------
- F1             : Показать / скрыть панель управления Spectator
- F9             : Сделать скриншот (Alpha PNG / 8x MSAA)
- F10            : Начать / остановить запись видео MP4 (60 FPS, реальное время 1:1)
- W / A / S / D  : Движение Вперед / Влево / Назад / Вправо в 3D пространстве
- E / Space      : Подняться вверх в 3D мире
- Q / Left Ctrl  : Опуститься вниз в 3D мире
- Left Shift     : Зажать для ускорения движения (Boost 3.5x)
- Правая кнопка  : Зажать и двигать мышь для вращения камеры (обзор 3D)

[ 3.3 ФИЗИЧЕСКИЕ 3D ОБЪЕКТЫ В СЦЕНЕ И ИЗОГНУТЫЙ ФОН (НАСТОЯЩИЙ 3D PARALLAX) ]
--------------------------------------------------------------------------------
Фон является реальным 3D объектом в игровом пространстве, создавая настоящую
глубину и эффект параллакса при перемещении камеры вокруг игрового поля:
- [ 🖼️ Curved Backdrop ] : Изогнутый 3D студийный фон (Циклорама).
  * Bend Sides (Wrap X)  : Изгиб по бокам (0° плоский -> 110° студия -> 240° полусфера).
  * Bend Top/Bottom (Y)  : Изгиб сверху и снизу (0° плоский -> 120° купол/чаша).
- [ 🌐 360° Sphere Dome ]: 3D сфера с инвертированной сеткой для 360° HDRI панорам.
- [ 🔲 Flat Wall ]       : Классическая плоская 3D стена / постер.
- Встроенный HDR парсер  : Чтение .hdr (RGBE) с тономаппингом Reinhard.
                           Также поддерживаются форматы PNG, JPG и BMP.
- Ручной ввод чисел      : Прямой ввод точных значений для Ширины (Scale X),
                           Высоты (Scale Y) и Глубины / Радиуса (Scale Z).
- [ 🎯 Place Behind View]: Мгновенно размещает 3D форму в 8 метрах перед камерой
                           и фиксирует её в мировых 3D координатах.
- Яркость (Light)        : Регулировка яркости и экспозиции от 0.05x до 5.0x.

[ 3.4 СКРИНШОТЫ И ЗАПИСЬ ВИДЕО MP4 1:1 В РЕАЛЬНОМ ВРЕМЕНИ ]
--------------------------------------------------------------------------------
- Скриншот F9 (PNG)     : Сглаживание 8x MSAA. Поддерживает прозрачный фон
                          (Disable Skybox + Alpha 0).
- Видеозапись F10 (MP4) : Захват финального кадра экрана Windows со всеми шейдерами,
                          пост-обработкой и интерфейсом.
- Синхронизация 1:1     : Точный расчет времени гарантирует, что 1 сек игры = 1 сек видео
                          (устраняет проблему ускоренного воспроизведения в VR).
- Асинхронный движок    : Фоновый поток отправляет кадры в FFmpeg с GPU аппаратным
                          ускорением NVENC H.264 (с переходом на CPU libx264).

[ 3.5 ПРЕСЕТЫ ТОЛЬКО ДЛЯ КАМЕРЫ (P1 - P4) ]
--------------------------------------------------------------------------------
- Save Cam P1 - P4 / Load Cam P1 - P4:
  * Сохраняет и загружает исключительно положение камеры (Pos, Rot, FOV, Speed).
  * НЕ сбрасывает и НЕ перезаписывает загруженный 3D фон, HDRI и настройки изгиба.

[ 3.6 ПОЛНОЕ СОХРАНЕНИЕ ВСЕХ НАСТРОЕК (SpectatorSettings.ini) ]
--------------------------------------------------------------------------------
- Автосохранение каждые 5 секунд, при выходе из игры или по кнопке [ 💾 Save Now ].
- При повторном запуске игры ПОЛНОСТЬЮ восстанавливается рабочее состояние:
  * Позиция, вращение, FOV и скорость камеры.
  * Активный файл изображения/HDRI и его положение в пространстве.
  * Размеры (Width/Height/Depth) и углы изгиба (Bend Sides & Top/Bottom).
  * Сохраненные пресеты камеры 1-4.
  * Значения в терминале Proxima (Уровень, Монеты, Жизни, Звезды).

[ 3.7 ТЕРМИНАЛ PROXIMA И ЧИТЫ В ИГРЕ ]
--------------------------------------------------------------------------------
- Быстрые кнопки: Set Level, Load Level, Load Menu, Set Coins, Set Lives,
  Set Stars, Win Level, Lose Level, Recenter Area, Reset Tutorials, Clear Save.
- Текстовая консоль: Запуск любых внутренних команд и методов Proxima.

================================================================================
   CAPTURES & SETTINGS STORAGE / SALVARE CAPTURI SI SETARI / СОХРАНЕНИЕ ФАЙЛОВ
================================================================================
- Screenshots  : Captures\Screenshots\
- Videos (MP4) : Captures\Videos\
- Custom Images: Captures\CustomSkyboxes\
- Settings INI : Captures\SpectatorSettings.ini
================================================================================
