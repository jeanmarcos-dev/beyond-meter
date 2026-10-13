## Why

La app de Windows solo tiene interfaz en inglés y ruso, mientras que los nombres de
skills y NPC del juego ya vienen en español en `data/`. Un jugador que habla
español ve los nombres del juego en su idioma, pero el resto de la app en inglés.
El instalador y las instrucciones tampoco están en español.

## What Changes

- Interfaz completa de la app de Windows en español neutro con tuteo: ventanas,
  bandeja, mensajes, nombres de clases y efectos de golpe, y las palabras de la
  línea de chat (KILL, "grupo", "#3 de 5"…).
- Selector de idioma con tres opciones (English, Español, Русский) en Ajustes. En
  la bandeja, el elemento que alternaba inglés y ruso pasa a ser un submenú con
  los tres idiomas.
- Fechas de la ventana de actualización en formato español.
- Notas de release con secciones por idioma: la ventana de actualización muestra
  la sección del idioma de la interfaz. Las notas del original, solo en inglés y
  ruso, se siguen mostrando igual.
- Instalador en español: asistente de Inno Setup y mensajes propios (Npcap,
  cerrar la app, borrar datos al desinstalar).
- `INSTALL.txt` con una sección en español.
- El idioma se sigue eligiendo a mano: un jugador nuevo arranca en inglés, como
  hoy.

## Capabilities

### New Capabilities

- `interface-languages`: idiomas de la interfaz de la app de Windows, cómo se
  eligen y qué textos cubren, incluidas las notas de la ventana de actualización.
- `installer-languages`: idiomas del instalador y de las instrucciones que se
  entregan con la app.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.App`: `UiText` (más un archivo nuevo con el español),
  `SettingsWindow`, `TrayIcon`, `App.xaml.cs`, `UpdateWindow`, `AppSettings`
  (documentación del valor `Language`).
- `src/AionMeter.Core/Updates/ReleaseNotes.cs`: secciones marcadas por idioma.
- `installer/AION2DpsMeter.iss` e `INSTALL.txt`.
- `tests/AionMeter.Tests`: compila los textos de la interfaz para testearlos.
- `README.md`: idiomas disponibles y formato de las notas de release.
- Sin cambios en el overlay de Linux, que ya está en español.
