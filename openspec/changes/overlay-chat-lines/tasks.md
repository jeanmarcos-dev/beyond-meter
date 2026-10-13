# Tasks

## 1. Línea de grupo con el formato compartido

- [x] 1.1 Escribir en `OverlayTests` los casos de 180 caracteres con ocho nombres largos, KILL/WIPE y sin invocaciones contra `ChatLine.Party`, y ver que los de límite fallan con `MeterView.CopyText`; verificar con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter OverlayTests`
- [x] 1.2 Reemplazar `MeterView.CopyText` por `ChatLine.Party` en la acción de copiar y borrar `CopyText` y sus tests; verificar que pasan los tests de la 1.1
- [x] 1.3 Ajustar `Copy_button_puts_the_fight_on_the_clipboard` al formato nuevo y añadir el caso "sin pelea no toca el portapapeles"; verificar con `dotnet test ... --filter OverlayWindowTests`

## 2. Línea de un jugador

- [x] 2.1 Test headless: con el panel de skills abierto sobre un compañero, el botón de copiar deja `ChatLine.Player` de ese compañero en el portapapeles; verificar que falla
- [x] 2.2 Añadir el botón al `SkillPanel` y la acción en `OverlayWindow`; verificar que pasa la 2.1
- [x] 2.3 Añadir `OverlayCommand.CopySelf` con su test de parseo y un test headless que copie la línea propia con el overlay fijado; verificar ambos

## 3. Uso del binario y cierre

- [x] 3.1 Hacer que el mensaje de uso se genere desde `OverlayCommand` y testear que nombra todos los comandos; verificar con `dotnet test ... --filter OverlayTests`
- [x] 3.2 Correr la suite completa y `dotnet format --verify-no-changes` sobre Overlay y Tests; verificar salida limpia
- [ ] 3.3 Publicar en `~/.local/share/aionmeter-overlay`, reaplicar `setcap` y pegar una línea real en el chat del juego; verificar que se ve entera
