# Tasks

## 1. Filtros y datos de cada fila

- [x] 1.1 Escribir tests de `MeterView.History` y de un `HistoryFilter` puro: combinación jefes+kills con recuento 3 de 10, fila con resultado, zona, duración, jugadores, DPS y puesto propios, y fallback al DPS del grupo sin puesto; verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter OverlayTests`
- [x] 1.2 Implementar `HistoryFilter` y ampliar `HistoryItemView` con los campos de `HistoryEntry`; verificar que pasan los tests de la 1.1

## 2. Navegación filtrada

- [x] 2.1 Tests de `MeterSession`: `Older`/`Newer` con "solo jefes" saltan las peleas sin jefe y vuelven a "En vivo" al pasar la más nueva; verificar que fallan
- [x] 2.2 Hacer que la sesión navegue sobre la lista filtrada; verificar que pasan la 2.1 y los tests de navegación existentes

## 3. Borrado

- [x] 3.1 Tests de `MeterSession.Delete`: borra el archivo, pasa a la siguiente pelea filtrada o a "En vivo", y no hace nada en vivo; verificar que fallan
- [x] 3.2 Implementar `MeterSession.Delete` sobre `HistoryStore.Delete`; verificar que pasa la 3.1
- [x] 3.3 Tests headless: "Borrar" pide confirmación en línea, confirmar borra, cancelar o esperar 5 s no borra, y el botón no aparece en vivo; verificar que fallan y luego implementar los controles hasta que pasen

## 4. Panel y cierre

- [x] 4.1 Añadir los dos filtros y el recuento al panel de historial con su test headless; verificar con `--filter OverlayWindowTests`
- [x] 4.2 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 4.3 Publicar, reaplicar `setcap` y borrar una pelea de demo desde el overlay; verificar que desaparece de `~/.local/share/aionmeter/fights`
