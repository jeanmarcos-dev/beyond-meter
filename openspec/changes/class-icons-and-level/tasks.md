# Tasks

## 1. Nivel en el Core

- [x] 1.1 Tests de motor: un `SelfIdentifiedEvent` con nivel 45 deja `Level = 45` en la fila propia de la instantánea y del detalle, un evento posterior con nivel 0 no lo borra, y una pelea guardada sin nivel carga con 0; verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter "EngineTests|HistoryTests"`
- [x] 1.2 Añadir `PlayerInfo.Level`, el nivel en `SetSelf`, `Combatant.Level` en `RefreshIdentity` y `Level` al final de `CombatantSnapshot` y `CombatantDetail`; verificar que pasa la 1.1

## 2. Íconos y nivel en el overlay

- [x] 2.1 Tests: `ClassIcons.For` devuelve un bitmap para las 9 clases y `null` para `Unknown`; `MeterView` arma "Nv 45" para el propio personaje y nada con nivel 0; verificar que fallan con `--filter OverlayTests`
- [x] 2.2 Enlazar los PNG como `AvaloniaResource`, implementar `ClassIcons` y el nivel en `RowView` y `DetailView`; verificar que pasa la 2.1
- [x] 2.3 Tests headless: la fila de un gladiador muestra su ícono y la de una clase desconocida no, y el encabezado del detalle muestra el ícono; verificar que fallan y luego implementar en `RowControl` y `SkillPanel` hasta que pasen

## 3. Cierre

- [x] 3.1 Comprobar que la app de Windows compila con los campos nuevos; verificar con `dotnet build src/AionMeter.App -p:EnableWindowsTargeting=true`
- [x] 3.2 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 3.3 Publicar, reaplicar `setcap` y revisar en el juego que los íconos y el nivel propio se ven bien; verificar con una captura de pantalla
