# Tasks

## 1. Grabación en caliente en el Core

- [x] 1.1 Test de `LiveCapture` con un dispositivo de archivo: empezar y detener la grabación sin cerrar el dispositivo produce un `.pcap` legible por `PcapReplaySource`; verificar que falla con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter LiveCapture`
- [x] 1.2 Añadir `StartRecording(dir)` y `StopRecording()` a `LiveCapture` reutilizando `OpenRecorder`; verificar que pasa la 1.1 y que el CLI `live --record` sigue funcionando

## 2. Grabar desde el overlay

- [x] 2.1 Tests: `record` alterna la grabación sin perder la pelea en curso y la cabecera muestra "REC" fijado o sin fijar; verificar que fallan con `--filter OverlayWindowTests`
- [x] 2.2 Implementar `OverlayCommand.Record`, el estado en `MeterSession` y el indicador; verificar que pasa la 2.1
- [ ] 2.3 Grabar una sesión real mientras se navega y comprobar con `tcpdump -r <archivo> 'port 80 or port 443' | wc -l` que no hay tráfico web; verificar que da 0

## 3. Repetir en el overlay

- [x] 3.1 Tests: `--replay` con un `.pcap` de prueba muestra la pelea esperada, `--fast` termina sin esperar el tiempo real, el historial, la caché de nombres y los timers no cambian, y un archivo inexistente sale con código distinto de 0; verificar que fallan
- [x] 3.2 Implementar el arranque con `PcapReplaySource`, la marca de repetición en la cabecera y el bloqueo de guardado; verificar que pasa la 3.1

## 4. Cierre

- [x] 4.1 Documentar `record`, `--replay` y `--fast` en docs/linux-overlay.md; verificar que los comandos documentados funcionan tal cual
- [x] 4.2 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
