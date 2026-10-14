## Why

Cuando una pelea va mal, el jugador no sabe si fue la rotación, el servidor o su
conexión, y hoy tiene que abrir otra herramienta para ver la latencia. El
medidor ya captura la conexión del juego, y en ella viaja el ping que el propio
cliente mide cada ~10 s: basta con leerlo para mostrarlo en el overlay, sin
enviar nada y con el mismo número que siente el juego, también detrás de un
acelerador de ping (ExitLag y similares).

## What Changes

- Decodificar la respuesta de ping del servidor (`Ping_RS`, opcode `0x0336`),
  que devuelve el timestamp con que el cliente envió el pedido.
- Calcular la latencia a partir de ese timestamp, que tiene dos formatos:
  - Global (EU/NA): contador de rendimiento del PC (`QueryPerformanceCounter`)
    en ms más el desplazamiento fijo de Unreal (16 777 216 s).
  - KR/TW: ms UTC desde la época de .NET.
  Con el contador local como fuente principal (QPC en Windows,
  `CLOCK_MONOTONIC_RAW` bajo Wine/Proton en Linux) y, como respaldo, el tiempo
  entre el pedido de ping capturado y su respuesta, más un desplazamiento
  aprendido para respuestas cuyo pedido no se vio.
- Mostrar el ping en la línea superior del overlay con un ícono de barras de
  señal y el valor (`42 ms`), con color según umbral: verde de 0 a 99 ms,
  amarillo de 100 a 170 ms, rojo desde 171 ms. Sin dato, `— ms`.
- Preferencia en *Ajustes → Overlay* para ocultar el ping (visible por
  defecto).
- Textos nuevos en inglés, español y ruso.
- Fuera de alcance (posible segunda etapa): RTT continuo de TCP, detección de
  congelamientos del servidor y de pérdida de paquetes, historial del ping por
  pelea.

## Capabilities

### New Capabilities

- `game-ping`: medición pasiva de la latencia del juego a partir de su propio
  intercambio de ping y su presentación en el overlay.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Core/Protocol`: manejo del opcode `0x0336` en el parser.
- `src/AionMeter.Core/Capture`: el pipeline tiene que ver también el sentido
  cliente→servidor de la conexión del juego para detectar los pedidos de ping;
  hoy solo se procesa el sentido servidor→cliente.
- Nuevo rastreador de ping en Core, independiente del reloj del sistema
  operativo (el reloj se inyecta), con unit tests.
- Acceso al contador de rendimiento: P/Invoke a `QueryPerformanceCounter` en
  Windows y a `clock_gettime(CLOCK_MONOTONIC_RAW)` en Linux.
- `src/AionMeter.Overlay`: indicador en la línea superior, preferencia nueva y
  su control en Ajustes.
- `src/AionMeter.Core/Text`: cadenas en en/es/ru.
- Las repeticiones de capturas grabadas (`.pcap`) no pueden usar el contador
  del PC de entonces; en ellas el ping sale solo del emparejamiento
  pedido/respuesta.
- Referencias del diseño: A2Tools DPS Meter (GPL-3.0) y AionPingMeter (MIT),
  citadas en `design.md`.
