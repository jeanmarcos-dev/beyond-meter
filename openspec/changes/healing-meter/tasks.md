# Tasks

## 1. Curación en el Core

- [x] 1.1 Escribir tests de motor en `EngineTests`: clérigo que solo cura aparece con daño 0, curar fuera de pelea no abre una, la curación de una invocación suma al dueño, curar a un NPC no cuenta y curar no alarga la pelea; verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter EngineTests`
- [x] 1.2 Dar de alta sanadores en `OnHeal` y añadir `Healing` y `Hps` a `CombatantSnapshot` en `BuildSnapshot`; verificar que pasan la 1.1 y todos los `EngineTests` previos
- [x] 1.3 Test de `HistoryTests`: una pelea guardada en el formato viejo se carga con curación 0 y una nueva conserva la curación tras guardar y leer; verificar que pasa

## 2. Vista de curación

- [x] 2.1 Tests de `MeterView`: filas por curación, sin jugadores con 0, porcentaje sobre el total del grupo; verificar que fallan con `--filter OverlayTests`
- [x] 2.2 Implementar el modo de vista en `MeterView` y la cabecera de columnas acorde; verificar que pasa la 2.1
- [x] 2.3 Tests headless: el botón y `heal` alternan la vista, el modo se mantiene al navegar el historial; verificar que fallan y luego añadir `OverlayCommand.Heal` y el botón hasta que pasen

## 3. Cierre

- [x] 3.1 Comprobar que la app de Windows compila con el campo nuevo; verificar con `dotnet build src/AionMeter.App -p:EnableWindowsTargeting=true`
- [x] 3.2 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 3.3 Publicar, reaplicar `setcap` y revisar una pelea de grupo real con sanador; verificar que aparece en la vista de curación
