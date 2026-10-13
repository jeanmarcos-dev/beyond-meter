# Design

## Context

- **`AionMeter.Overlay`** (Avalonia 11.3, `net10.0`) arranca en
  `OverlayApp.Start`. Su configuración es `OverlaySettings`, un record
  serializado en `~/.config/aionmeter/overlay.json` con lectura tolerante por
  clave. Los datos van en `~/.local/share/aionmeter`. Las piezas de Linux son:
  - `X11Window`: click-through con XShape y posición raíz;
  - `CommandChannel`: socket Unix en `XDG_RUNTIME_DIR`, que también sirve de
    instancia única;
  - `BossAlert`: `notify-send` y `canberra-gtk-play`;
  - `ShortcutHints`: lee los atajos de GNOME por `dconf`.

  Tiene unos 60 textos fijos en español en unos 12 archivos. Su interfaz se
  testea con `Avalonia.Headless.XUnit`.
- **`AionMeter.App`** (WPF) tiene lo que le falta al overlay: bandeja
  (WinForms `NotifyIcon`), `HotkeyManager` (RegisterHotKey), `Updater` con
  `UpdateWindow`, `SettingsWindow`, `SkillIcons` y `BossPortraits` (descarga con
  caché, devuelven `ImageSource`), `ChatCopy`, `Log`, el aviso de Npcap, GS/CP y
  el tamaño de fila. Guarda en `%AppData%\AionMeter`: `settings.json`
  (`AppSettings`), `fights\`, `names-cache.json`, `boss-timers.json`,
  `field-boss-learned.json`, `portraits\`, `captures\` y `logs\`.
- **Ya portables:** `UiText`, `UiText.Es` y `AppSettings` no dependen de WPF.
  `HistoryStore`, `NameCache` y `BossTimers` están en Core, así que los datos
  tienen el mismo formato en las dos apps.
- **Instalador:** usa `AppExe` = `BeyondMeter.exe` y el AppId del original. El
  updater acepta un instalador cuyo `ProductName` sea "Beyond Meter".

## Goals / Non-Goals

**Goals:**
- Una sola base de código de interfaz, testeable en Linux con Headless, con lo
  propio de cada sistema detrás de una capa de plataforma chica.
- Que el paso de WPF al overlay sea una actualización más para el jugador de
  Windows, sin perder datos ni ajustes.
- Llegar al reemplazo por etapas que se puedan publicar: Linux recibe las mejoras
  apenas están, Windows cambia una sola vez, al final.

**Non-Goals:**
- Auto-instalación en Linux: el overlay solo avisa que hay versión nueva y abre
  la página del release.
- macOS.
- Rediseñar la apariencia: se conserva la del overlay actual, y las ventanas
  nuevas (Ajustes, Actualización) siguen su estilo.
- Mantener la app WPF después del reemplazo.

## Decisions

- **Lo compartido pasa a Core.** `UiText`, `UiText.Es` y `AppInfo` se mueven a
  `AionMeter.Core` (namespace `AionMeter.Core.Text`). También van allí el modelo
  de configuración y la lógica sin interfaz de `SkillIcons`, `BossPortraits`
  (descarga y caché que devuelven rutas de archivo) y `Updater` (cuándo buscar,
  descargar, verificar). Mientras exista, la app WPF los usa desde Core. Se
  descarta una biblioteca nueva solo para esto: Core ya tiene `ChatWords` y
  `ReleaseNotes`, que son de la misma naturaleza.

- **Configuración: un solo `settings.json` con el esquema de `AppSettings`,
  ampliado y leído clave por clave.**
  - Se le suman las claves del overlay que no tiene: `Locked`,
    `LockedOpacity`, `BossAlertMinutes`, `HistoryMinSeconds`, tamaños del
    desglose, vista daño/curación, etc.
  - Se lee con la técnica tolerante de `OverlaySettings.Parse`.
  - En Windows el archivo es el mismo `%AppData%\AionMeter\settings.json` de la
    app WPF, así que migrar desde WPF no requiere conversión: las claves nuevas
    toman su valor por defecto.
  - En Linux pasa a `$XDG_CONFIG_HOME/aionmeter/settings.json`. Si no existe y
    hay `overlay.json`, se importa una vez, con `Language = "es"` porque esa era
    la interfaz que veía el jugador. `overlay.json` no se toca.
  - Se descarta conservar dos formatos (`overlay.json` en Linux y
    `settings.json` en Windows), porque la ventana de Ajustes tendría que
    escribir los dos.

- **Capa de plataforma `IDesktopPlatform`, con implementaciones `LinuxDesktop` y
  `WindowsDesktop`.** Cubre:
  - rutas de configuración y datos;
  - click-through de una ventana;
  - registro de atajos globales;
  - notificación con sonido;
  - abrir un URL o una carpeta;
  - la ruta del canal de comandos.

  `OverlayApp` elige la implementación según `OperatingSystem.IsWindows()`. Los
  tests usan un `FakeDesktop`. Lo que hoy está en `X11Window`, `BossAlert` y
  `ShortcutHints` pasa a `LinuxDesktop` sin cambiar su comportamiento.

- **El overlay compila para dos targets:** `net10.0` (Linux) y
  `net10.0-windows10.0.19041.0` (Windows). El target de Windows habilita las
  notificaciones de WinRT y un `AssemblyName` `BeyondMeter`. Linux conserva
  `aionmeter-overlay`, que usan su systemd y su documentación. El código de
  Windows que usa WinRT (el toast) va en archivos que solo compilan en ese
  target. El P/Invoke de Win32 (`Win32Native`) y `WindowsDesktop` compilan en
  los dos targets detrás de la interfaz `IWin32`, para testear su lógica en
  Linux con un `IWin32` falso. Se descarta compilar un único `net10.0` con
  P/Invoke para todo, porque las notificaciones de Windows sin WinRT obligarían
  a duplicar la bandeja.

- **Click-through en Windows con estilos Win32.** Sobre el handle de
  `TryGetPlatformHandle()` se activan `WS_EX_LAYERED | WS_EX_TRANSPARENT`, igual
  que hoy en `OverlayWindow.xaml.cs` de WPF.

- **Atajos en Windows con `RegisterHotKey`** sobre la ventana del overlay, y el
  mensaje `WM_HOTKEY` recibido con `Win32Properties.AddWndProcHookCallback`.
  El parser de atajos de `HotkeyManager.TryParse` pasa a Core y se testea en
  Linux. No hay hooks de teclado. En Linux los atajos siguen siendo del
  escritorio (los comandos por socket), y `ShortcutHints` los muestra.

- **Bandeja con `TrayIcon` y `NativeMenu` de Avalonia** en los dos sistemas. En
  Linux usa StatusNotifierItem por DBus. Si no hay área de notificación (GNOME
  sin extensión), el overlay sigue con sus comandos. Se descarta seguir con
  `NotifyIcon` de WinForms, porque arrastra WinForms al overlay.

- **Notificaciones:** en Windows, toasts de WinRT con el AppUserModelID
  `JeanmarcosDev.BeyondMeter`, con el sonido `Notification.Reminder`. Antes del
  primer toast, el overlay registra ese ID en
  `HKCU\Software\Classes\AppUserModelId` (nombre e ícono), así que funciona
  también sin acceso directo. El instalador igual lo fija en `[Icons]`. En Linux, lo de hoy (`notify-send` y
  `canberra-gtk-play`). Si el toast falla (por ejemplo, en el zip portátil sin
  acceso directo), se muestra una ventana propia de aviso, chica y sin foco, en
  la esquina.

- **Comandos e instancia única:** se mantiene el socket Unix también en Windows.
  .NET lo soporta desde Windows 10 1803. Su ruta es
  `%LocalAppData%\BeyondMeter\overlay.sock`, y abrir la app de nuevo envía
  `show`. Se descartan los named pipes: obligan a un segundo transporte y los
  tests de Linux no los cubrirían.

- **Textos:** los literales del overlay pasan a propiedades de `UiText`, con
  inglés, español y ruso. El test de cobertura de `spanish-localization` se
  extiende al ruso.

- **Reemplazo en dos etapas de release:**
  1. Mientras se porta, cada release publica el overlay nuevo para Linux y
     sigue publicando WPF para Windows.
  2. Con la paridad verificada en Windows, `build-release.ps1` publica el
     overlay para `win-x64` como `BeyondMeter.exe`, se borra
     `src/AionMeter.App` y los tests de identidad e idiomas apuntan al overlay.

  El CI corre `dotnet test` también en `windows-latest` desde la etapa 1, para
  la parte de Windows.

## Risks / Trade-offs

- [`AddWndProcHookCallback` o `TrayIcon` de Avalonia 11.3 no cubren algo que
  hace falta] → Una de las primeras tareas es un spike en Windows (por CI y a
  mano). Si falla, se usa una ventana Win32 oculta propia para `WM_HOTKEY`.
- [Los toasts de WinRT no funcionan sin AppUserModelID (zip portátil)] → Se
  muestra la ventana propia de aviso.
- [Bandeja ausente en GNOME] → Los comandos y atajos del escritorio cubren todo.
  La documentación lo dice.
- [Conflictos de merge con upstream cuando se borre la App: modify/delete en
  cada release] → Se resuelven manteniendo el borrado. Las novedades de
  interfaz de upstream se portan a mano, y el README explica el procedimiento.
- [La copia WPF se actualiza a un exe distinto en tecnología pero con el mismo
  nombre] → El instalador reemplaza la carpeta entera (los archivos de WPF que
  sobran se borran con `[InstallDelete]` por patrón) y el `ProductName` sigue
  siendo "Beyond Meter". Se verifica con una actualización real desde la 1.x
  WPF.
- [Proyecto largo con dos UIs vivas mientras dura] → Las funciones nuevas
  durante la transición se hacen solo en el overlay. La app WPF solo recibe
  correcciones.
- [Escrituras concurrentes si alguien corre la app WPF y el overlay a la vez
  sobre `%AppData%\AionMeter`] → La instancia única del overlay no ve a la de
  WPF, así que en Windows el overlay también toma el mutex
  `AionMeter.SingleInstance` de la app WPF.

## Resultado del spike en Windows

Probado a mano por el usuario en Windows con la build del CI (artifact
`beyond-meter-overlay-win-x64`, `--demo`). Funcionó todo:

- **Click-through:** `WS_EX_LAYERED | WS_EX_TRANSPARENT` más
  `SetLayeredWindowAttributes` con alfa 255 sobre la ventana de Avalonia. El
  overlay se sigue viendo y los clics pasan a la ventana de atrás. Al
  desactivarlo se restaura el estilo original y vuelve a recibir el mouse.
- **Atajos:** `RegisterHotKey` con `MOD_NOREPEAT` y `WM_HOTKEY` recibido con
  `Win32Properties.AddWndProcHookCallback` funcionan con otra ventana en
  foco. No hace falta la ventana Win32 oculta del plan B.
- **Bandeja:** `TrayIcon` con `NativeMenu` muestra el menú, y el clic
  izquierdo muestra u oculta el overlay.
- **Toast:** aparece con nombre y sonido sin acceso directo, gracias al
  registro del AppUserModelID en HKCU. La ventana propia de aviso queda solo
  como respaldo si el toast lanza una excepción.
- **Instancia única:** el socket Unix en `%LocalAppData%\BeyondMeter` sirve
  en Windows. Abrir el exe de nuevo muestra el overlay existente.
- **Assets:** los URI `avares://` usan el nombre del ensamblado en tiempo de
  ejecución, porque en Windows el ensamblado se llama `BeyondMeter`.

Cambios de enfoque: el código Win32 compila en los dos targets (ver
Decisiones) y el toast no depende del acceso directo del instalador.

## Migration Plan

1. **Etapas 1 a 5 (ver tasks):** las funciones llegan al overlay y se publican
   en Linux. Windows sigue con WPF.
2. **Etapa 6:** con la paridad verificada a mano en Windows, el release `2.0.0`
   cambia el payload de Windows al overlay. Las copias WPF se actualizan solas.
3. **Rollback:** si la `2.0.0` falla en Windows, se publica una `2.0.1` con el
   payload WPF. Para eso, el borrado de `src/AionMeter.App` va en un commit
   aparte y posterior al primer release con el overlay, así se puede revertir.
