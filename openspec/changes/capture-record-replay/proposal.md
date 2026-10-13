## Why

Cuando una cifra del overlay no cuadra (un jugador sin nombre, una pelea que se
corta antes de tiempo, una skill mal atribuida), no hay forma de volver a ver
lo que pasó: el tráfico se procesa y se pierde. La CLI ya puede grabar y
repetir capturas, pero el overlay no, así que no se puede reproducir un fallo
con la misma vista en que se vio. Grabar desde el overlay y repetir la
grabación en él permite diagnosticar sin volver a jugar la pelea.

## What Changes

- Comando `record` para empezar y detener la grabación del tráfico del juego a
  un `.pcap` en `~/.local/share/aionmeter/captures/`, con indicador "REC"
  visible mientras graba.
- Opción `--replay <archivo>` para abrir el overlay sobre una grabación en vez
  de la captura en vivo, a velocidad real o acelerada (`--fast`).
- Una repetición no guarda peleas en el historial, no cambia la caché de
  nombres ni los timers de jefes, y se distingue en la cabecera.
- La grabación solo contiene el tráfico del juego que ya filtra la captura,
  nunca el resto de la red.

## Capabilities

### New Capabilities

- `capture-recording`: grabar el tráfico del juego desde el overlay y volver a
  reproducirlo en él para revisar o diagnosticar peleas.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Overlay`: arranque con fuente de repetición, comando y estado
  de grabación, indicador en cabecera.
- Usa `LiveCaptureOptions.RecordDirectory` y `PcapReplaySource` del Core. Hoy
  `RecordDirectory` solo se lee al abrir el dispositivo, así que empezar a
  grabar en caliente requiere un cambio menor en `LiveCapture`.
- Las grabaciones pueden pesar decenas de MB por hora; no se borran solas.
