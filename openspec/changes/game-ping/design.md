## Context

Motivación en `proposal.md`; comportamiento en `specs/game-ping/spec.md`.

Estado actual que condiciona el diseño:

- El tiempo de cada segmento es la marca de libpcap/Npcap en ms Unix
  (`LiveCapture.cs:346`), igual en vivo y en repetición (`PcapReplaySource`).
  No se configura el tipo de timestamp del driver.
- El filtro BPF (`host … and port …`) deja pasar los dos sentidos, pero
  `PacketPipeline` solo procesa los flujos que se bloquean por el heartbeat
  `0E 00 36`, y eso ocurre únicamente en servidor→cliente. Los segmentos
  cliente→servidor quedan en `PreLock` hasta que se podan.
- El parser despacha por `switch (opcode)` (`PacketParser.cs:39`) con opcodes
  `0xAABB` donde `AA` es el primer byte (`Opcodes.cs`). `0x0336` no existe; el
  cuerpo llega sin los 2 bytes del opcode. El `TcpReassembler` ya descarta
  duplicados, así que una respuesta retransmitida no se parsea dos veces.
- Los eventos (`GameEvent(long TimeMs)`) llegan a `MeterSession` por
  `EventDecoded`. El overlay redibuja cada 250 ms y arma la línea superior con
  `MeterView.Header(...)` → `HeaderView`; a la derecha están `_recording`,
  `_limit` y `_time` (`OverlayWindow.cs:477`).
- Las preferencias viven en `UserSettings` (`settings.json`); `ShowBossPanel`
  es el ejemplo completo de un interruptor (declaración, `Parse`,
  `SettingsWindow.BuildOverlay`, `Collect`).
- Core apunta solo a `net10.0`; corre igual en Windows y en Linux.

Referencias externas estudiadas: A2Tools DPS Meter (`combat/ping_tracker.rs`,
GPL-3.0) y AionPingMeter (`Program.cs`, MIT). El formato del timestamp Global
(QPC en ms + 16 777 216 000) está verificado por A2Tools contra una captura
del 2026-10-01; el de KR/TW, por varios medidores.

## Goals / Non-Goals

**Goals:**

- Un rastreador de ping puro, sin reloj del sistema ni E/S, que reciba eventos
  y un reloj inyectado, para poder probar cada escenario de la spec con
  números fijos.
- Que el ping funcione igual con captura en vivo, en loopback (aceleradores)
  y en repetición, sin ramas por plataforma fuera del reloj.

**Non-Goals:**

- Reensamblar o descifrar el sentido cliente→servidor: solo se mira el tamaño
  de los frames de cada segmento.
- Cambiar el tipo de timestamp de Npcap (`HOST_HIPREC_UNSYNCED`), como hace
  AionPingMeter. La precisión de ms de la marca Unix basta para un número que
  se muestra entero.
- Guardar el ping en el historial de peleas.

## Decisions

### 1. El ping viaja como eventos, el cálculo vive en un rastreador aparte

El parser emite `PingReplyEvent(TimeMs, long ClientStamp)` al decodificar
`0x0336` (cuerpo: `00 00`, `int64` LE del cliente, `int64` LE del servidor).
El pipeline emite `PingRequestEvent(TimeMs)` al detectar un pedido y
`GameFlowLockedEvent(TimeMs)` al bloquear un flujo de juego nuevo. Un
`PingTracker` en `AionMeter.Core/Ping` consume esos tres eventos y expone
`Current(long nowMs)` (null sin dato).

*Alternativa descartada:* calcular el ping dentro del parser o del pipeline.
Mezcla reloj y estado de sesión con la decodificación, y la repetición
necesitaría otro camino. Con eventos, `PcapReplaySource` funciona sin cambios
y el rastreador se prueba sin paquetes.

### 2. Detección del pedido de ping en el sentido cliente→servidor

Cuando un flujo servidor→cliente se bloquea, el pipeline marca su flujo
inverso (misma clave con origen y destino intercambiados) como "lado
cliente". Cada segmento con datos de ese flujo se revisa por separado, sin
reensamblar:

- El segmento SHALL poder recorrerse entero como frames de longitud varint
  desde su primer byte, y alguno debe medir 12–13 bytes (12 con el formato
  KR/TW, 13 con el Global). El cuerpo está cifrado, pero ningún otro mensaje
  del cliente tiene ese tamaño, y el pedido sale cada ~10 s.
- O contiene las firmas de un relay que envuelve el frame:
  `05 23 0C 0F` o `05 23 0D 10`.

Las reglas son las de A2Tools. El tamaño físico del frame se calcula como en
`FrameDecoder`: `len + bytesDelVarint − 4`, varint incluido. Un pedido Global
es `10` seguido de 12 bytes (13 en total), igual que en los tests de A2Tools.
La regla se reutiliza con `Wire.TryVarint`, sin copiar la búsqueda de
re-sincronización del decodificador.

*Alternativa descartada:* bloquear también el flujo cliente→servidor y
pasarlo por `FrameDecoder`. Ese decodificador espera texto plano y
re-sincroniza byte a byte, lo que en tráfico cifrado inventa frames de 12–13
bytes.

### 3. Cálculo en el rastreador, por orden de preferencia

Para cada `PingReplyEvent`:

1. **Contador de rendimiento** (si hay reloj y no está desconfiado):
   `enviado = stamp − 16 777 216 000`. Debe estar en `[0, contadorAhora]`.
   `rtt = llegada − (enviado + (paredAhora − contadorAhora))`, con contador y
   pared leídos juntos en el momento de procesar el evento.
2. **Emparejamiento**: cada pedido de los últimos 9 999 ms implica una
   diferencia `pedido.TimeMs − stamp`. Si hay uno, es esa. Si hay varios, se
   toma el que coincide (±15 ms) con la diferencia aprendida o con alguna de
   las del ping anterior. Si coincide con la aprendida, se actualiza; si
   coincide con el ping anterior, se aprende. Si no coincide con la aprendida,
   esta se descarta, porque saltó uno de los relojes.
3. **Formato KR/TW**: `rtt = llegada − (stamp − 62 135 596 800 000)`.
4. **Diferencia aprendida**: `rtt = llegada − (stamp + diferencia)`.

Si (1) y (2) existen y difieren en más de 25 ms, gana (2) y se cuenta un
fallo; con dos fallos seguidos el contador queda desconfiado hasta el próximo
`GameFlowLockedEvent`. Toda muestra fuera de `[1, 9999]` se descarta.

*Por qué este orden:* el contador es exacto y no necesita ver el pedido, pero
en Linux su equivalencia con el reloj del juego no está verificada. El
emparejamiento no depende de ningún reloj y lo valida. El formato KR/TW se
deja detrás del emparejamiento porque un ajuste de hora de Windows lo
desplaza.

### 4. Reloj del contador inyectado: `IPerfClock`

`IPerfClock.Read()` devuelve `(long CounterMs, long WallMs)` leídos juntos.

- **Windows:** `Stopwatch.GetTimestamp()` y `Stopwatch.Frequency`, que en
  Windows son exactamente `QueryPerformanceCounter`. No hace falta P/Invoke.
- **Linux:** `clock_gettime(CLOCK_MONOTONIC_RAW)` por P/Invoke a `libc`.
  Wine implementa QPC sobre ese reloj; `Stopwatch` usa `CLOCK_MONOTONIC`, que
  NTP ajusta y que se aleja del RAW con las horas de encendido.
- **Repetición:** sin reloj (`null`). El contador de una captura vieja no
  tiene relación con el de hoy.

`OverlayApp` pasa `PerfClocks.ForThisMachine()` a `MeterSession`, que lo
descarta en modo repetición y al repetir un archivo. Los tests pasan un reloj
falso. En repetición, la vigencia de 30 s se mide contra el último evento de
la captura, no contra la hora actual.

### 5. Ciclo de vida

`PingTracker` guarda la última muestra válida y su hora de captura.
`Current(nowMs)` devuelve null si pasaron más de 30 000 ms desde esa muestra
(tres pings perdidos seguidos). `GameFlowLockedEvent` borra la muestra, los
pedidos pendientes, la diferencia aprendida y la desconfianza del contador.
Detener la captura borra lo mismo. El `nowMs` lo pasa `MeterSession` con el
mismo reloj que usa para el resto del overlay.

### 6. Presentación

`MeterView.Ping(int?)` devuelve un `PingView` propio (texto y `PingLevel`:
`None`, `Good`, `Fair`, `Poor`), separado de `HeaderView` porque el ping no
depende de la pelea ni del historial. Decide el texto (`42 ms` / `— ms`) y el
nivel por los umbrales: ≤ 99, ≤ 170, ≥ 171. `OverlayWindow` agrega al `StackPanel` de la derecha,
delante de `_time`, un ícono de cuatro barras (rectángulos alineados abajo,
de altura creciente, dibujados con controles y sin imagen, para que tomen el
color del nivel) y el `TextBlock` `_ping`. Verde y rojo salen de la paleta
existente (`Win`, `LimitRed`); el amarillo es uno propio más limón
(`#E6E03A`), porque el ámbar de los límites se confunde con el dorado del
título. Gris (`Muted`) para `None`. Se oculta si `UserSettings.ShowPing` es falso. Se muestra también sin
pelea, a diferencia del reloj de la pelea.

### 7. Preferencia y textos

`UserSettings.ShowPing` (por defecto `true`), leída con `Bool(...)` en `Parse`,
con su `CheckBox` en `SettingsWindow.BuildOverlay` y en `Collect`, siguiendo
`ShowBossPanel`. Cadenas nuevas en `UiText` (en), `UiText.Es.cs` (es) y el
bloque `Ru`: la etiqueta de la opción, por ejemplo "Show ping", "Mostrar
ping", "Показывать пинг".

## Risks / Trade-offs

- [La regla de 12–13 bytes deja de valer tras un parche del juego] → El
  contador (Windows) no depende de ella; en Linux el ping quedaría sin dato en
  vez de dar un número falso. Los tamaños son constantes con nombre, fáciles
  de actualizar.
- [`CLOCK_MONOTONIC_RAW` no es el reloj de QPC en alguna versión de
  Wine/Proton] → La desconfianza automática pasa al emparejamiento tras dos
  pings; el costo es que el primer valor tarda ~20 s.
- [Un cambio del formato del timestamp en un parche] → Fuera de los rangos
  válidos no se informa nada; el indicador queda en `— ms`.
- [La marca de Npcap se toma con el reloj de pared y Windows lo corrige en
  plena sesión] → El cálculo por contador lee pared y contador juntos al
  procesar, igual que A2Tools; el emparejamiento solo usa marcas de la misma
  captura.
- [Un segmento cliente→servidor puede traer el pedido junto con otros frames]
  → La regla acepta el segmento si recorre limpio y alguno de sus frames mide
  12–13 bytes.

## Open Questions

- ~~Si `CLOCK_MONOTONIC_RAW` coincide con el contador del juego bajo Proton.~~
  Resuelta el 2026-10-14 con una captura Global de 70 s bajo Proton
  Experimental: los 8 pedidos salieron a la red 0,6–1,6 ms después del
  timestamp del juego leído como `CLOCK_MONOTONIC_RAW`, y el ping por contador
  coincidió con el emparejado dentro de 1 ms (61–70 ms). En ese cliente el
  pedido mide 12 bytes, no 13.
