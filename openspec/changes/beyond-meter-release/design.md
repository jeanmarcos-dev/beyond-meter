# Design

## Context

- `main` está en upstream v0.2.8, con el overlay de Linux y los cambios de Core
  encima, todavía sin commitear. `origin` apunta al upstream.
- La identidad de la app de Windows está repartida en varios lugares:
  - `AppInfo.Name`;
  - literales en `UiText` (inglés y ruso);
  - títulos en `OverlayWindow.xaml`, `HistoryWindow.xaml` y
    `BreakdownWindow.xaml`;
  - `AssemblyName` del csproj;
  - los `#define` y mensajes de `installer/AION2DpsMeter.iss`;
  - nombres de archivo y `Repair-VersionStrings` en `build-release.ps1`;
  - user agents de `Updater` y `BossPortraits`, y la carpeta de descarga
    de `Updater`.
- `Updater` ejecuta un instalador descargado solo si su `ProductName` es
  `AppInfo.Name`. `UpdateFeed.Repository` es una constante con el repo del
  upstream.
- `UpdateFeed.ParseVersion` corta sufijos y `Normalize` se queda con
  mayor.menor.parche.
- `UpdateFeed.Parse` reconoce los archivos del release por forma, no por nombre:
  `.exe` que contiene `Setup`, y `-win-x64.zip`.
- El instalador ya cierra con WMI y `taskkill` la copia que corre desde
  `{app}`, pero solo busca el nombre de exe actual.
- `DesktopShortcutWanted` decide si poner el acceso directo del escritorio
  mirando el `.lnk` con el nombre actual.
- La exclusión mutua usa los nombres `AionMeter.SingleInstance` y
  `AionMeter.ShowOverlay`, que no dependen del nombre del producto.
- El proyecto de tests es `net10.0` y no referencia `AionMeter.App`
  (`net10.0-windows`). Desde Linux solo se compila la App con
  `EnableWindowsTargeting`; no se testea.
- No existe `.github/` ni `Directory.Build.props`. La versión vive solo en
  `AionMeter.App.csproj`.

## Goals / Non-Goals

**Goals:**
- Que toda la identidad visible cambie desde la primera versión. El chequeo de
  `ProductName` obliga a que la app y el instalador cambien juntos.
- Que un release sea reproducible desde un tag, sin necesitar una máquina
  Windows local.
- Que cada traída de cambios del upstream choque lo menos posible con la
  identidad del fork.

**Non-Goals:**
- Firmar los ejecutables.
- Llevar las funciones del overlay de Linux (curación, replay, nivel) a la app
  WPF.
- Renombrar el overlay de Linux: binario, socket, rutas XDG y unidad systemd se
  quedan como están.
- Actualizar el kit de la página de descargas (`docs/site`,
  `tools/make-site-kit.ps1`) y `README.ru.md`.
- Auto-update en Linux.

## Decisions

- **Fork con `gh repo fork`, remotos `origin` (fork) y `upstream`.** Así GitHub
  muestra "forked from" y `git fetch upstream` trae sus releases. La
  alternativa, un repo nuevo con `push`, pierde el vínculo visible y el crédito
  automático. Los forks de GitHub nacen con Actions desactivado; activarlo es un
  paso manual del dueño.

- **La identidad sale de `AppInfo.Name` siempre que se pueda.** Los literales de
  `UiText` se escriben como interpolación de `AppInfo.Name` y los títulos XAML
  se fijan desde código o con `x:Static`. Así, una traída del upstream que
  agregue un texto con "AION2 DPS Meter" se detecta como un literal nuevo y no
  como una regresión silenciosa. La alternativa de reemplazar cada literal por
  "Beyond Meter" multiplica los puntos de conflicto en cada merge.

- **Test de identidad por inspección de fuentes.** La App no se puede testear
  desde el proyecto de tests. Por eso se agrega un test que recorre
  `src/AionMeter.App`, `installer/` y `build-release.ps1` y falla si aparece
  "AION2 DPS Meter" o "AION2DpsMeter" fuera de una lista explícita de
  excepciones: el crédito al original y los nombres viejos que el instalador
  borra. También protege contra regresiones en futuros merges. Lo que este test
  no cubre (reemplazo, limpieza y actualización silenciosa) se verifica a mano
  en Windows.

- **Mismo AppId, nuevo AppName, nuevo exe.**
  - `AppId` no cambia. Por eso Inno trata al original como versión anterior,
    reutiliza su carpeta (`UsePreviousAppDir`) y deja una sola entrada de
    desinstalación. `Updater.UninstallKey` tampoco cambia.
  - `AppName` pasa a "Beyond Meter" y `AppExe` a `BeyondMeter.exe`.
  - En una instalación limpia, `DefaultDirName` queda en
    `{autopf}\Beyond Meter`. Sobre el original se conserva la carpeta
    `AION2 DPS Meter`, que es lo que pide el reemplazo.

- **Limpieza del original en dos pasos.**
  - En `PrepareToInstall` se cierra cualquier `AION2DpsMeter.exe` que corra
    desde `{app}`, sin preguntar, igual que hace Restart Manager con la copia
    propia. Para eso `CloseRunningMeter` recibe el nombre de exe como
    parámetro.
  - `[InstallDelete]` borra `{app}\AION2DpsMeter.exe` y los
    `AION2 DPS Meter.lnk` del menú Inicio y del escritorio.
  - `DesktopShortcutWanted` acepta también el `.lnk` con el nombre viejo, para
    que quien tenía el acceso directo del original reciba el nuevo.
  - Se descarta conservar `AION2DpsMeter.exe` como nombre del exe: el proceso y
    el exe seguirían diciendo el nombre del original.

- **Exclusión mutua sin cambios.** Con los mismos nombres de mutex y evento,
  el original y Beyond Meter no pueden correr a la vez en la misma sesión, y
  abrir uno con el otro abierto muestra el que ya corre.

- **Versión común en `Directory.Build.props`.** Un solo `<Version>1.0.0</Version>`
  en la raíz, que heredan App, Overlay y Cli, más `<UpstreamVersion>0.2.8</UpstreamVersion>`
  con la base del upstream. `build-release.ps1` lee ambos de ahí. Se descarta
  mantener la versión en `AionMeter.App.csproj`, porque el overlay quedaría sin
  versión o con una copia que se desincroniza.

- **Workflow `release.yml` disparado por tags `v*`.** Tiene cuatro jobs:
  - `verify` (ubuntu): comprueba que el tag sea `v` + `<Version>`, y corre
    `dotnet test` y `dotnet format --verify-no-changes` sobre los proyectos
    de tests y Cli.
  - `windows` (windows-latest): instala Inno Setup 7 en versión fija y corre
    `build-release.ps1`.
  - `linux` (ubuntu): corre el script de empaquetado de Linux.
  - `publish`: espera a los tres anteriores y crea el release con `gh release
    create`.

  `build-release.ps1` sigue siendo la única receta de Windows: sirve igual en
  local y en CI. Las notas salen de `release-notes/<versión>.md`, que es
  obligatorio. El job `publish` les agrega una línea con la base del upstream
  leída de `<UpstreamVersion>`.

- **Linux: publish self-contained en carpeta, no single-file.** Un script de
  empaquetado hace `dotnet publish -r linux-x64 --self-contained` del overlay y
  arma el tarball con el overlay, `data/`, `LICENSE`, `install.sh` y los
  archivos de `packaging/linux` del servicio.
  - Se descarta single-file porque extrae las librerías nativas de Avalonia a
    un directorio temporal. Con capacidades de archivo (`setcap`) el proceso
    corre en modo seguro, y esa extracción y las variables de entorno se
    vuelven frágiles.
  - `install.sh` copia a `~/.local/share/aionmeter-overlay` (la misma ruta que
    documenta `docs/linux-overlay.md`), enlaza `~/.local/bin/aionmeter-overlay`,
    aplica `sudo setcap cap_net_raw,cap_net_admin+eip` y avisa si no encuentra
    libpcap.
  - No instala el servicio systemd: eso sigue siendo opcional con
    `install-service.sh`.
  - Ajustes e historial viven en `~/.config` y `~/.local/share/aionmeter`, fuera
    de la carpeta que se reemplaza.

- **Trabajo local en un commit propio antes del fork.** El overlay de Linux y
  los cambios de Core van en un commit separado de este change, y este change
  se commitea por etapas: identidad, actualizaciones, empaquetado y CI,
  documentación. Se descarta un commit por cada change de OpenSpec del
  overlay: sus archivos se cruzan (por ejemplo, `OverlayWindow.cs`) y
  separarlos a mano no aporta.

## Risks / Trade-offs

- [El chequeo de `ProductName` rechaza el primer instalador si la app y el `.iss`
  no cambian juntos] → El test de identidad cubre los dos lados, y la
  verificación manual incluye una actualización real de `1.0.0` a una `1.0.1`
  de prueba.
- [Un jugador vuelve a instalar el original encima de Beyond Meter] → Como
  comparten AppId, el original reemplaza a Beyond Meter y desde entonces se
  actualiza desde el upstream. Es la contracara aceptada del reemplazo; el
  README lo explica.
- [SmartScreen marca el instalador sin firma como "editor desconocido"] → El
  original tiene el mismo problema. El README explica "Más información →
  Ejecutar de todos modos".
- [Inno Setup 7 en el runner de Windows: la forma de instalarlo y la versión
  pueden cambiar] → Versión fija y verificada con checksum. Si falla, el job
  falla y no se publica nada a medias.
- [Cada merge del upstream puede volver a meter literales con el nombre viejo]
  → El test de identidad lo detecta en ese mismo merge.
- [`setcap` requiere sudo y se pierde en cada actualización] → `install.sh` lo
  reaplica siempre y lo dice en pantalla.
- [La spec de `overlay-chat-lines` describe el formato de línea anterior a
  upstream 0.2.5] → Se corrige como tarea de este change, para que el archivado
  de ese change no deje una spec falsa.

## Migration Plan

1. Commitear el trabajo local y crear el fork. Los tags `v0.x` del upstream
   quedan en el fork sin releases, así que `releases/latest` no devuelve nada
   hasta `v1.0.0` y la app solo informa que está al día.
2. Implementar identidad, actualizaciones y empaquetado, y verificar en local.
3. Activar Actions en el fork, publicar `v1.0.0`, verificar a mano en Windows
   (sobre el original y en limpio) y en Linux (con el tarball).
4. Rollback: si `v1.0.0` sale mal, se borra el release (y el tag) con `gh` y se
   publica `v1.0.1`. Las copias instaladas solo ven releases publicados.
