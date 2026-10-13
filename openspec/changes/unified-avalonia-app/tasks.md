# Tasks

## 1. Base compartida en Core

- [x] 1.1 Mover `UiText.cs`, `UiText.Es.cs` y `AppInfo.cs` a `src/AionMeter.Core/Text/` (namespace `AionMeter.Core.Text`), quitar los `<Compile Include>` enlazados del proyecto de tests y ajustar los `using` de la App y del test de identidad; verificar que la suite pasa y que la App compila con `-p:EnableWindowsTargeting=true` sin warnings
- [x] 1.2 Mover el parser de atajos (`HotkeyManager.TryParse`) a Core con tests de los formatos aceptados y rechazados (`Ctrl+Shift+D`, `Alt+F9`, vacío, tecla desconocida); verificar que pasan y que la App compila
- [x] 1.3 Crear en Core el modelo de configuración unificado (claves de `AppSettings` más las del overlay) con lectura tolerante por clave y escritura atómica; tests de valores por defecto, rangos, valores ilegibles, round-trip y lectura de un `settings.json` real de la app WPF; verificar que pasan
- [x] 1.4 Mover a Core la descarga y caché de íconos de skills y retratos de jefes (devolviendo rutas de archivo), con tests sobre un `HttpMessageHandler` falso; dejar en la App solo la conversión a `ImageSource`; verificar que pasan y que la App compila
- [x] 1.5 Mover a Core la lógica de `Updater` que no es de interfaz (cuándo buscar, versión omitida, descarga y verificación, si la copia está instalada), con tests; verificar que pasan y que la App compila

## 2. Capa de plataforma

- [x] 2.1 Definir `IDesktopPlatform` (rutas, click-through, atajos, notificación, abrir URL o carpeta, ruta del canal) y un `FakeDesktop` de tests; mover `X11Window`, `BossAlert`, `ShortcutHints` y las rutas XDG a `LinuxDesktop` sin cambiar comportamiento; verificar que la suite del overlay sigue en verde
- [x] 2.2 Hacer que el overlay compile para `net10.0` y `net10.0-windows10.0.19041.0`, con `AssemblyName` `BeyondMeter` en Windows; agregar al CI un job de `dotnet test` en `windows-latest`; verificar con `actionlint` y una corrida de CI en una rama, previa confirmación del usuario
- [x] 2.3 Spike en Windows: click-through con `WS_EX_TRANSPARENT`, `WM_HOTKEY` con `AddWndProcHookCallback`, `TrayIcon` con menú y un toast de WinRT; verificar con una build de CI que el usuario prueba a mano, y registrar en `design.md` el resultado y cualquier cambio de enfoque

## 3. Idiomas en el overlay

- [x] 3.1 Pasar cada texto fijo del overlay a propiedades de `UiText` en inglés, español y ruso, y extender el test de cobertura al ruso; verificar que no queda ningún literal visible en `src/AionMeter.Overlay` (test que lo busque) y que pasan los tests Headless
- [x] 3.2 Aplicar el idioma en caliente en todas las ventanas del overlay y cambiar el chip de idioma por el menú de tres idiomas; test Headless que cambie a ruso y compruebe textos del overlay, el desglose y el panel de timers; verificar que pasa

## 4. Configuración, Ajustes y bandeja

- [x] 4.1 Usar el modelo unificado en el overlay: en Linux, `settings.json` en `XDG_CONFIG_HOME/aionmeter` con importación única desde `overlay.json` (idioma español); en Windows, `%AppData%\AionMeter\settings.json`; tests de migración de los dos casos y de que el archivo viejo queda intacto; verificar que pasan
- [x] 4.2 Ventana de Ajustes en Avalonia con las ocho secciones, Guardar y Cancelar, y opciones por sistema; tests Headless de guardar, cancelar y aplicación en caliente (modo de medición, idioma, opacidad); verificar que pasan
- [x] 4.3 Bandeja con `TrayIcon` y el menú completo (incluido el submenú de idioma), funcionando aunque no haya área de notificación; tests de las acciones del menú con `FakeDesktop`; verificar que pasan y que el overlay arranca sin bandeja en una sesión sin StatusNotifier
- [x] 4.4 Actualizar `docs/linux-overlay.md` con Ajustes, bandeja, idiomas y el nuevo `settings.json`; verificar que las claves documentadas coinciden con el modelo

## 5. Paridad de funciones

- [x] 5.1 Columnas GS/CP con su opción y tamaño de fila (50–100 %, también con Ctrl + rueda); tests Headless; verificar que pasan
- [x] 5.2 Barra de HP del jefe, "contado desde" y límites de tiempo del jefe y la instancia en la línea superior, como en la app WPF; tests Headless con snapshots que los traigan; verificar que pasan
- [x] 5.3 Íconos de skills en el desglose y retratos de jefes, con su opción de descarga; tests Headless con imágenes en caché; verificar que pasan
- [x] 5.4 Copiar la línea de un jugador y la tabla para Discord desde el overlay y el desglose; tests del contenido del portapapeles; verificar que pasan
- [ ] 5.5 Aviso de Npcap al arrancar en Windows cuando falta, con enlace a npcap.com; test con `FakeDesktop`; verificar que pasa
- [ ] 5.6 Logs en la carpeta de datos con rotación como en la app WPF; tests; verificar que pasan
- [ ] 5.7 Banner y ventana de actualización con novedades en el idioma de la interfaz; en Windows, descarga e instalación como la app WPF; en Linux, solo el aviso y el enlace al release; tests con un feed falso; verificar que pasan
- [ ] 5.8 Revisar que el historial, el desglose (precisión, rotación, defensa), los timers con alertas y la demo, el replay y la grabación cubren lo de la app WPF, completando lo que falte con sus tests; verificar con la lista de la spec `single-app`

## 6. Windows

- [ ] 6.1 `WindowsDesktop`: click-through, atajos globales con aviso de atajo ocupado, toasts con sonido y aviso propio de respaldo, rutas en `%AppData%\AionMeter`, canal en `%LocalAppData%\BeyondMeter` y mutex `AionMeter.SingleInstance`; tests que corran en el job de Windows; verificar en CI
- [ ] 6.2 Instalador: `AppUserModelID` en los accesos directos y `[InstallDelete]` de los archivos de la app WPF que no trae el overlay; verificar con los tests del instalador y una build de CI
- [ ] 6.3 Hacer que `build-release.ps1` publique el overlay para `win-x64` y retargetear los tests de identidad e idiomas al overlay; verificar con un tag de prueba con sufijo, previa confirmación del usuario, que el instalador y el zip traen `BeyondMeter.exe` del overlay

## 7. Reemplazo

- [ ] 7.1 Verificación manual en Windows (a cargo del usuario): instalar el pre-release sobre la 1.x WPF con historial, timers y ajustes; probar cada función de la lista de paridad, la actualización automática desde la 1.x y que los atajos, el click-through, la bandeja y las alertas funcionan con el juego abierto
- [ ] 7.2 Publicar `v2.0.0` con notas en los tres idiomas, previa confirmación del usuario; verificar que el release tiene los archivos de Windows y Linux y que una 1.x instalada se actualiza sola
- [ ] 7.3 En un commit aparte, posterior al release, borrar `src/AionMeter.App`, sacarlo de la solución y de los scripts, y documentar en el README cómo traer cambios del upstream sin la App; verificar que la suite, el formato y un tag de prueba pasan
