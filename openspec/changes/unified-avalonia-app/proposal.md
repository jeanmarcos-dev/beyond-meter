## Why

Beyond Meter tiene dos interfaces distintas sobre el mismo motor: la app WPF de
Windows, heredada del upstream, y el overlay Avalonia de Linux. Se ven y se
comportan distinto, cada función nueva hay que hacerla dos veces, y las
funciones propias de Linux (curación, grabación y replay, nivel) no llegan a
Windows. El objetivo es tener una sola app, la de Avalonia, que se vea y funcione
igual en los dos sistemas.

## What Changes

- El overlay Avalonia pasa a ser la única app, en Windows y en Linux, con la
  apariencia actual de Linux.
- Antes de reemplazar a la app WPF en Windows, el overlay incorpora todo lo que
  hoy solo tiene WPF:
  - ventana de Ajustes;
  - icono en la bandeja con su menú;
  - atajos globales;
  - click-through en Windows;
  - auto-update y ventana de actualización;
  - aviso de Npcap;
  - íconos de skills y retratos de jefes;
  - columnas GS/CP y tamaño de fila;
  - copiar el resultado de un jugador y la tabla para Discord;
  - logs.
- Interfaz del overlay en inglés, español y ruso, con los mismos textos que hoy
  usa WPF. **BREAKING** en Linux: el idioma de la interfaz deja de ser español
  fijo y pasa a ser elegible. Las instalaciones existentes arrancan en español.
- Configuración unificada en un solo formato para los dos sistemas. En Windows
  se leen los ajustes, el historial y los timers que ya están en
  `%AppData%\AionMeter`, así que actualizar desde la app WPF no pierde nada. En
  Linux se migra `overlay.json`.
- Capa de plataforma con implementaciones para Linux (X11, notify-send) y
  Windows (estilos de ventana Win32, RegisterHotKey, notificaciones).
- **BREAKING**: cuando hay paridad completa, el release de Windows publica el
  overlay Avalonia como `BeyondMeter.exe`, con el mismo instalador e identidad, y
  se borra `src/AionMeter.App`. Desde entonces, del upstream solo se traen
  cambios de Core; sus novedades de interfaz se portan a mano.

## Capabilities

### New Capabilities

- `single-app`: una sola app para Windows y Linux, dónde guarda sus datos en
  cada sistema, cómo reemplaza a la app WPF y qué funciones debe tener antes del
  reemplazo.
- `app-settings`: ventana de Ajustes, formato único de configuración y migración
  desde los formatos anteriores.
- `desktop-integration`: bandeja, atajos globales, click-through,
  notificaciones, instancia única y comandos externos, en cada sistema.
- `overlay-languages`: idiomas de la interfaz del overlay y su cobertura.

### Modified Capabilities

(ninguna; las capacidades de los changes en curso todavía no están en
`openspec/specs/`)

## Impact

- **`src/AionMeter.Overlay`**: casi todos los archivos. Se agregan ventana de
  Ajustes, bandeja, ventana de actualización y capa de plataforma.
- **`src/AionMeter.Core`**: recibe `UiText` (con `UiText.Es.cs`), el modelo de
  configuración y la lógica de actualización, retratos e íconos, que hoy viven
  en la App.
- **`src/AionMeter.App`**: se borra en la última etapa.
- **Empaquetado y CI:**
  - `build-release.ps1` publica el overlay para Windows;
  - el instalador sigue igual salvo el payload;
  - el CI corre los tests también en Windows.
- **Tests:** los tests de identidad e idiomas pasan de la App al overlay.
- **Documentación:** `README.md`, `INSTALL.txt` y `docs/linux-overlay.md`.
- **Dependencias:** ninguna nueva; Avalonia ya trae `TrayIcon` y los ganchos de
  Win32.
- **Changes en curso:** las specs de `spanish-localization` (UI de WPF) y de
  `overlay-chat-lines` quedan superadas cuando se borre la App. Hay que archivar
  esos changes antes que este.
