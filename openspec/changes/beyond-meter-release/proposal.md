## Why

El overlay de Linux y los cambios de Core solo existen en esta copia local: no hay
un lugar desde donde un jugador pueda descargarlos, y el upstream
(`cyberbadger6969/aion2-dps-meter`) es de otra persona, así que no se pueden
publicar releases ahí. Hace falta un fork propio, `jeanmarcos-dev/beyond-meter`,
que publique la app de Windows y el overlay de Linux. Sin cambios, la app de
Windows de ese fork se auto-actualizaría desde el upstream y pisaría la build
del fork.

## What Changes

- Fork en GitHub `jeanmarcos-dev/beyond-meter`, creado como fork del upstream
  para que el crédito quede visible y se puedan traer sus cambios.
- La app de Windows pasa a llamarse **Beyond Meter** en ventanas, bandeja,
  menú Inicio, instalador y mensajes; el exe pasa a `BeyondMeter.exe`.
- El instalador **reemplaza** al AION2 DPS Meter original: mantiene su AppId y
  la carpeta `%AppData%\AionMeter`, así que instala encima y conserva ajustes,
  historial y timers. Borra el exe y los accesos directos del original para que
  no quede una copia vieja que se siga abriendo.
- **BREAKING**: el auto-update consulta los releases de
  `jeanmarcos-dev/beyond-meter` y deja de ver los del upstream.
- Numeración propia desde `1.0.0`, compartida por la app de Windows y el overlay
  de Linux; las notas de cada release dicen en qué versión del upstream se basa.
- Releases automáticos en GitHub Actions al publicar un tag `v*`: instalador y
  zip portátil de Windows, tarball del overlay de Linux con un script de
  instalación, y zip del código fuente.
- README con la identidad del fork, crédito al upstream y enlaces de descarga
  del fork.

## Capabilities

### New Capabilities

- `app-identity`: nombre visible de la app de Windows, reemplazo del original en
  la misma máquina y crédito al upstream.
- `app-updates`: de dónde saca la app de Windows sus actualizaciones y qué
  instalador acepta.
- `release-packaging`: qué archivos trae cada release, cómo se numeran y cómo se
  construyen, para Windows y para Linux.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.App`: `AppInfo`, `UiText`, títulos de ventanas XAML, `Updater`,
  `BossPortraits` (user agent), `AionMeter.App.csproj` (`AssemblyName`,
  versión).
- `src/AionMeter.Core/Updates/UpdateFeed.cs`: repositorio del feed.
- `installer/AION2DpsMeter.iss` y `build-release.ps1`: nombre, exe, nombres de
  archivo, limpieza del original.
- Nuevo: workflow de GitHub Actions, script de empaquetado e instalación de
  Linux, `Directory.Build.props` con la versión común.
- `README.md` y `docs/linux-overlay.md`.
- El overlay de Linux conserva su binario `aionmeter-overlay`, sus rutas y su
  unidad de systemd; solo cambia cómo se distribuye.
- El change en curso `overlay-chat-lines` describe la línea de un jugador con el
  formato anterior a upstream 0.2.5; su spec queda desactualizada respecto del
  código y hay que corregirla.
- Requisito previo: el trabajo local (overlay de Linux y cambios de Core sobre
  upstream 0.2.8) se commitea antes de crear el fork.
