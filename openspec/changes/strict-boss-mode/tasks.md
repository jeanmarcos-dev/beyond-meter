# Tasks

## 1. Regla del modo Jefes en el medidor

- [x] 1.1 Escribir tests en `EngineTests` con `TargetMode.BossOnly`: golpear un mob sin pelea no abre segmento, `LiveFight()` es null y `Snapshot(null)` es null; tras matar al jefe, golpear mobs varios minutos deja la pelea del jefe en `Snapshot(null)` y no dispara `EncounterFinished`; golpear a otro jefe abre una pelea nueva; los adds no suman ni retrasan el cierre por inactividad; una curación sin jefe no abre pelea; un muñeco abre pelea; con `TargetMode.All` un mob sí abre pelea. Verificar que los del modo estricto fallan
- [x] 1.2 Filtrar en `CombatTracker.OnDamage` el daño a NPC que no son jefe con `BossOnly`, mantener el cierre de una pelea sin jefe al golpear a un jefe y corregir el comentario de `TargetMode.BossOnly`. Verificar que los tests de 1.1 pasan
- [x] 1.3 Correr toda la suite y pasar a `TargetMode.All` los tests existentes que daban por hecho que sin jefe se medía todo y no prueban el modo. Verificar que `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj` pasa completo

## 2. Historial

- [x] 2.1 Escribir un test de sesión con `BossOnly` y `HistoryMode.All`: un minuto de mobs no guarda nada y una pelea de 30 s contra un jefe sí se guarda. Verificar que pasa

## 3. Textos y documentación

- [x] 3.1 Cambiar `MeasureBoss` a `Boss only`, `Solo jefes` y `Только боссы` en `UiText`, `UiText.Es` y el bloque ruso. Verificar con `UiTextTests` y un test de que Ajustes en español ofrece `Solo jefes` y `Todo lo que golpeas`
- [x] 3.2 Describir el modo estricto en `docs/linux-overlay.md` (comando `mode`, clave `TargetMode`), `README.md` y `README.ru.md` (fila `BOSS / ALL`). Verificar leyendo que ninguno diga que el modo Jefes mide todo sin jefe

## 4. Verificación

- [x] 4.1 `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj` y `dotnet format --verify-no-changes` (salvo los dos archivos con errores previos)
- [ ] 4.2 Reproducir con `aionmeter-overlay --replay` una grabación con jefe y mobs después, o probar en el juego, y comprobar que el overlay sigue mostrando al jefe y que el historial no recibe peleas de mobs
