# Tasks

## 1. Decodificar la respuesta de ping

- [x] 1.1 Escribir en `ProtocolTests` los tests de `0x0336`: un cuerpo `00 00` + `int64` LE + `int64` LE emite `PingReplyEvent` con el timestamp del cliente y el `TimeMs` del parser; un cuerpo de menos de 10 bytes no emite nada. Verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter ProtocolTests`
- [x] 1.2 Agregar `Opcodes.PingReply = 0x0336` (con su nombre en `Opcodes.Name`), `PingReplyEvent` en `GameEvents.cs` y su `case` en `PacketParser.Handle`. Verificar que pasan los tests de la 1.1

## 2. Pedidos de ping y cambio de conexión en el pipeline

- [x] 2.1 Escribir tests de la regla de detección del pedido: un segmento `10` + 12 bytes es pedido; uno de 12 bytes con varint correcto también; un segmento que no recorre limpio como frames no lo es, aunque contenga un frame de 13 bytes; las firmas de relay `05 23 0C 0F` y `05 23 0D 10` lo son. Verificar que fallan
- [x] 2.2 Implementar la regla con `Wire.TryVarint` y el tamaño `len + bytesDelVarint − 4`. Verificar que pasan los tests de la 2.1
- [x] 2.3 Escribir tests de `PacketPipeline` (al estilo de `EngineTests.cs:516`): tras bloquear un flujo servidor→cliente se emite `GameFlowLockedEvent`; un segmento del flujo inverso que es pedido emite `PingRequestEvent` con su `TimeMs`; los pedidos de antes del bloqueo y los de flujos ajenos no emiten nada; el flujo inverso no se reensambla ni pasa por el parser. Verificar que fallan
- [x] 2.4 Marcar el flujo inverso al bloquear y emitir `PingRequestEvent` y `GameFlowLockedEvent` por el sink del parser. Verificar que pasan los tests de la 2.3 y que el resto de la suite sigue pasando

## 3. Rastreador de ping

- [x] 3.1 Escribir `PingTrackerTests` con un `IPerfClock` falso, uno por escenario de la spec: Global por contador (45 ms), KR/TW sin pedido (80 ms), emparejamiento sin contador (64 ms), diferencia aprendida tras dos pings, salto de 2 s del reloj (se informa sin el salto y la respuesta sin pedido no se informa hasta reaprender), contador desconfiado tras dos desacuerdos de más de 25 ms, muestras < 1 o > 9999 ms descartadas, sin dato a los 30 s, `GameFlowLockedEvent` que borra el estado, varios pedidos en la ventana resueltos por la diferencia conocida. Verificar que fallan con `--filter PingTracker`
- [x] 3.2 Implementar `IPerfClock` y `PingTracker` en `src/AionMeter.Core/Ping` según el orden del diseño (contador, emparejamiento, KR/TW, diferencia aprendida). Verificar que pasan los tests de la 3.1
- [x] 3.3 Implementar los relojes reales: Windows con `Stopwatch.GetTimestamp`/`Frequency`; Linux con `clock_gettime(CLOCK_MONOTONIC_RAW)` por P/Invoke a `libc`. Agregar un test que lea el reloj de la plataforma actual dos veces y compruebe que el contador avanza y que la pared es cercana a `DateTimeOffset.UtcNow`. Verificar que pasa en Linux

## 4. Sesión y overlay

- [x] 4.1 Escribir tests: `MeterSession` en vivo crea el rastreador con el reloj de la plataforma, en repetición sin reloj, y le pasa los eventos de ping; `MeterView.Ping` devuelve `42 ms`/`Good`, `60 ms`/`Fair`, `120 ms`/`Poor` y `— ms`/`None`. Verificar que fallan
- [x] 4.2 Conectar el rastreador en `MeterSession` (también lo borra al detener la captura) y agregar `PingView` y `MeterView.Ping`. Verificar que pasan los tests de la 4.1
- [x] 4.3 Escribir un test headless de `OverlayWindow`: el indicador muestra el texto y el color de cada nivel, se ve sin pelea en curso y desaparece con `ShowPing` en falso. Verificar que falla
- [x] 4.4 Agregar `_ping` al `StackPanel` de la línea superior, delante de `_time`, con colores de la paleta existente. Verificar que pasa el test de la 4.3

## 4b. Ícono de señal y umbrales nuevos

- [x] 4b.1 Actualizar los tests de `MeterView.Ping` a los umbrales nuevos (99 verde, 100 y 170 amarillo, 171 rojo) y el test headless del overlay para que exija el ícono `PingBars` con cuatro barras de altura creciente en el color del nivel y un amarillo distinto del dorado. Verificar que fallan
- [x] 4b.2 Cambiar los umbrales en `MeterView.Ping`, agregar el ícono de barras delante del valor y el amarillo propio. Verificar que pasan los tests de la 4b.1 y mirar el overlay instalado con el juego abierto

## 5. Preferencia y textos

- [x] 5.1 Extender `UserSettingsTests`: `ShowPing` vale `true` por defecto, se guarda y se vuelve a leer, y un `settings.json` sin la clave queda en `true`. Verificar que falla
- [x] 5.2 Agregar `UserSettings.ShowPing`, su `CheckBox` en `SettingsWindow.BuildOverlay` y su lectura en `Collect`, siguiendo `ShowBossPanel`. Verificar que pasan la 5.1 y los tests de `SettingsWindow`
- [x] 5.3 Agregar la etiqueta de la opción en `UiText` (en), `UiText.Es.cs` (es) y el bloque `Ru`. Verificar que pasan `UiTextTests` con los tres idiomas
- [x] 5.4 Documentar el indicador de ping en `README.md` y `README.ru.md` (Features) y en `docs/linux-overlay.md` la nota de que en Linux el primer valor puede tardar ~20 s si el contador no coincide. Verificar que los enlaces del README siguen resolviendo

## 6. Cierre

- [x] 6.1 Correr `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj` y `dotnet format --verify-no-changes`. Verificar salida limpia
- [x] 6.2 Jugar una sesión en Linux (Proton) con la grabación activada. Verificar que el overlay muestra un ping parecido al del juego, anotar en el log si el contador quedó desconfiado (cierra la pregunta abierta del diseño) y repetir la captura grabada para comprobar que el ping sale por emparejamiento. Hecho el 2026-10-14: el juego aún no muestra su ping, así que la referencia fue el emparejamiento sobre la captura de 70 s (61–70 ms, contador dentro de 1 ms); el overlay instalado mostró 66 ms
- [ ] 6.3 Jugar una sesión en Windows, con y sin acelerador de ping. Verificar que el ping del overlay coincide con el del juego dentro de ±10 ms
