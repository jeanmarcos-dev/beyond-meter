# Tasks

## 1. Timers en el Core

- [x] 1.1 Mover `BossTimers.cs` a `src/AionMeter.Core/Timers/` con namespace del Core y actualizar los `using` de la app de Windows; verificar con `dotnet build src/AionMeter.Core` y `dotnet build src/AionMeter.App -p:EnableWindowsTargeting=true`
- [x] 1.2 Añadir tests del Core para los escenarios del spec que no dependen de la UI: hora de la lista del juego, muerte con intervalo conocido, timers por servidor, "debería estar vivo", marcar muerto, un solo aviso por reaparición y jefe vivo sin aviso; verificar con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter BossTimer`

## 2. Cableado en el overlay

- [x] 2.1 Test de `MeterSession`: los eventos de jefe del tracker llegan a los timers y los del demo no tocan el archivo; verificar que falla
- [x] 2.2 Crear los timers en `OverlayApp` con rutas XDG y conectarlos en `MeterSession` saltando demo y repetición; verificar que pasa la 2.1

## 3. Avisos

- [x] 3.1 Tests del sondeo con un `IBossAlert` doble y reloj inyectado: aviso a la hora menos la antelación, sin repetir, sin aviso para no vigilados; verificar que fallan
- [x] 3.2 Implementar el sondeo y `DesktopBossAlert` con `notify-send` y `canberra-gtk-play`, más su caso "ejecutable ausente"; verificar que pasan los tests y lanzar un aviso real con un jefe de prueba

## 4. Panel y resumen

- [x] 4.1 Tests headless: `timers` abre y cierra el panel, cada fila muestra el estado esperado, las acciones vigilar/intervalo/muerto/quitar persisten y la línea de resumen aparece con el panel cerrado; verificar que fallan
- [x] 4.2 Implementar el panel, las acciones, `OverlayCommand.Timers` y la línea de resumen; verificar que pasan con `--filter OverlayWindowTests`
- [x] 4.3 Añadir la antelación de aviso a las preferencias con su rango y documentarla en el README; verificar el test de parseo

## 5. Cierre

- [x] 5.1 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 5.2 Publicar, reaplicar `setcap`, abrir el mapa de un jefe de campo en el juego y comprobar que el panel se llena con la lista del juego
