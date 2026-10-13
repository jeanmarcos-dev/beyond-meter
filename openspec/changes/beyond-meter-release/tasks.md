# Tasks

## 1. Base y fork

- [x] 1.1 Commitear el trabajo local (overlay de Linux, cambios de Core sobre upstream 0.2.8, changes de OpenSpec previos) en un commit propio, previa confirmación del usuario; verificar con `git status` limpio fuera de `openspec/changes/beyond-meter-release` y con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj` en verde sobre ese commit
- [x] 1.2 Corregir el escenario "Copiar a otro jugador" de `openspec/changes/overlay-chat-lines/specs/overlay-chat-copy/spec.md` al formato de upstream 0.2.5 (`<nombre> #<puesto> of <total>: <DPS>/s, …`); verificar con `openspec validate overlay-chat-lines` y comparando con lo que esperan `OverlayWindowTests` y `OverlayTests`
- [x] 1.3 Crear el fork con `gh repo fork cyberbadger6969/aion2-dps-meter --fork-name beyond-meter --remote` y renombrar los remotos a `origin` (fork) y `upstream`, previa confirmación del usuario; verificar con `git remote -v` y `gh repo view jeanmarcos-dev/beyond-meter --json parent`

## 2. Versión común

- [x] 2.1 Crear `Directory.Build.props` con `<Version>1.0.0</Version>` y `<UpstreamVersion>0.2.8</UpstreamVersion>` y quitar `<Version>` de `AionMeter.App.csproj`; agregar un test que lea la versión del ensamblado de Core y del overlay y compruebe que es `1.0.0`; verificar que el test pasa y que la App compila con `-p:EnableWindowsTargeting=true` con versión de ensamblado `1.0.0`

## 3. Identidad de la app de Windows

- [x] 3.1 Escribir el test de identidad que recorre `src/AionMeter.App`, `installer/` y `build-release.ps1` y falla ante "AION2 DPS Meter" o "AION2DpsMeter" fuera de la lista de excepciones (crédito al original y nombres viejos que borra el instalador); verificar que hoy falla
- [x] 3.2 Cambiar `AppInfo.Name` a "Beyond Meter", reescribir los literales de `UiText` (inglés y ruso) como interpolación de `AppInfo.Name`, fijar los títulos de `OverlayWindow`, `HistoryWindow` y `BreakdownWindow` desde `AppInfo.Name`, y cambiar `AssemblyName` a `BeyondMeter` y los user agents de `Updater` y `BossPortraits` y la carpeta de descarga a `BeyondMeter`; verificar que la App compila sin warnings con `-p:EnableWindowsTargeting=true`
- [x] 3.3 Agregar el crédito al proyecto original (enlace y GPL-3.0) en la app, donde hoy se muestran el aviso legal y la versión; verificar que la App compila y que el texto figura en la lista de excepciones del test
- [x] 3.4 En `installer/AION2DpsMeter.iss`: `AppName` "Beyond Meter", `AppExe` `BeyondMeter.exe`, URLs del fork, `OutputBaseFilename` `BeyondMeter-Setup-v…` y mensajes `CloseRunning` en inglés y ruso, sin tocar `AppId`; parametrizar `CloseRunningMeter` con el nombre de exe, cerrar `AION2DpsMeter.exe` en `PrepareToInstall`, agregar `[InstallDelete]` del exe y los `.lnk` viejos y hacer que `DesktopShortcutWanted` acepte el `.lnk` viejo; verificar con el test de identidad
- [x] 3.5 En `build-release.ps1`: leer `Version` de `Directory.Build.props`, nombres `BeyondMeter-*` (instalador, zip, fuente y copias con nombre fijo) y `Repair-VersionStrings` con "Beyond Meter"; verificar que pasan el test de identidad y la suite completa

## 4. Actualizaciones desde el fork

- [x] 4.1 Tests en `UpdateTests`: `UpdateFeed.LatestReleaseApi` y `ReleasesPage` apuntan a `jeanmarcos-dev/beyond-meter`, `IsProductName` acepta "Beyond Meter" y rechaza "AION2 DPS Meter", y `Parse` reconoce `BeyondMeter-Setup-v1.1.0.exe` y `BeyondMeter-v1.1.0-win-x64.zip`; verificar que el primero falla
- [x] 4.2 Cambiar `UpdateFeed.Repository` a `jeanmarcos-dev/beyond-meter`; verificar que pasan los tests de la 4.1 y la suite completa

## 5. Paquete de Linux

- [x] 5.1 Escribir `packaging/linux/install.sh`: copia a `~/.local/share/aionmeter-overlay` reemplazando la versión previa, enlaza `~/.local/bin/aionmeter-overlay`, aplica `sudo setcap cap_net_raw,cap_net_admin+eip` y avisa si falta libpcap; verificar con `shellcheck` y ejecutándolo dos veces seguidas sobre una carpeta de prueba (`HOME` temporal, sin `setcap`)
- [x] 5.2 Escribir `packaging/linux/build-tarball.sh`: `dotnet publish` self-contained `linux-x64` del overlay y tarball `beyond-meter-v<versión>-linux-x64.tar.gz` (más la copia con nombre fijo) con el overlay, `data/`, `LICENSE`, `install.sh` y los archivos del servicio; verificar con `shellcheck`, listando el tarball con `tar tzf` y arrancando `aionmeter-overlay --demo` desde una extracción limpia
- [x] 5.3 Actualizar `docs/linux-overlay.md`: instalación desde el tarball del release como camino principal y compilación desde el código como alternativa; verificar que los comandos documentados coinciden con los de `install.sh`

## 6. Releases automáticos

- [x] 6.1 Crear `release-notes/1.0.0.md` en inglés con el contenido de la primera versión del fork; verificar que `ReleaseNotes.Lines` lo parsea sin perder secciones (test en `UpdateTests`)
- [x] 6.2 Crear `.github/workflows/release.yml` con los jobs `verify` (tag = `v` + `Version`, tests, `dotnet format --verify-no-changes`), `windows` (Inno Setup 7 en versión fija con checksum, consultando su documentación vigente, y `build-release.ps1`), `linux` (`build-tarball.sh`) y `publish` (`gh release create` con los archivos y las notas de `release-notes/<versión>.md` más la línea "Based on upstream <UpstreamVersion>"); verificar con `actionlint`
- [x] 6.3 Probar el workflow en el fork con un tag de prueba `v1.0.0-test`, que publica un pre-release sobre la `Version` `1.0.0`, previa confirmación del usuario y con Actions activado en el fork; verificar que el release tiene los cinco archivos de Windows, los dos de Linux y las notas con la línea del upstream, y después borrar el release y el tag de prueba

## 7. Documentación del fork

- [x] 7.1 Actualizar `README.md`: nombre Beyond Meter, crédito al original con enlace y GPL-3.0, badges y enlaces de descarga del fork (`BeyondMeter-Setup.exe`, `BeyondMeter-win-x64.zip`, tarball de Linux), aviso de que instala encima del original y de que reinstalar el original lo reemplaza, y receta de release por tag; verificar que ningún enlace de descarga apunta al upstream con `grep -n cyberbadger README.md`

## 8. Publicación y verificación integral

- [x] 8.1 Publicar `v1.0.0` con un tag, previa confirmación del usuario; verificar que el workflow termina en verde y el release tiene todos los archivos
- [ ] 8.2 Verificación manual en Windows (a cargo del usuario): instalar `BeyondMeter-Setup.exe` sobre el AION2 DPS Meter original con peleas guardadas, con el original abierto y con acceso directo en el escritorio; verificar una sola entrada "Beyond Meter" en Aplicaciones instaladas, sin accesos directos ni exe viejos, con el historial visible y "Beyond Meter" en títulos y bandeja
- [ ] 8.3 Verificación manual en Windows (a cargo del usuario): instalación limpia sin el original, y actualización desde la app de `1.0.0` a un `v1.0.1` publicado; verificar que el auto-update instala y vuelve a abrir Beyond Meter en `1.0.1`
- [ ] 8.4 Verificación en Linux: descargar el tarball del release, ejecutar `install.sh` y abrir el overlay con el juego; verificar que `aionmeter-overlay` captura sin root y que los ajustes y el historial previos siguen ahí
