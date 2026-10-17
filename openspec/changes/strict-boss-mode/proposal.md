## Why

El modo `Jefes`, que es el predeterminado, mide solo al jefe mientras hay uno en
pelea, pero sin jefe vuelve a medir todo. Por eso, al matar un jefe, el primer
mob normal que golpeas abre una pelea nueva: el resultado del jefe desaparece de
la pantalla y, si esa pelea dura lo suficiente, también se guarda en el
historial. Quien quiere ver y guardar solo jefes no tiene forma de evitarlo.

## What Changes

- **BREAKING** (comportamiento): el modo `Jefes` pasa a ser estricto. El daño a
  un NPC que no es jefe nunca abre ni alarga una pelea, haya o no un jefe en
  pelea. Ya no se recurre a medir todo cuando no hay jefe.
- Después de una pelea contra un jefe, el overlay sigue mostrando ese resultado
  aunque se golpeen mobs normales, hasta que empiece la pelea contra otro jefe o
  se reinicie a mano.
- Con el modo `Jefes` solo se guardan en el historial peleas contra jefes, sea
  cual sea la opción `Qué peleas` de Ajustes. Con `Todo`, esa opción se aplica
  como hoy.
- Pegarle a mobs normales con el modo `Jefes` ya no cuenta como pelea para que
  el overlay se muestre solo.
- El modo `Todo` no cambia. Quedan dos modos, `Jefes` y `Todo`, en el botón de la
  barra, en Ajustes y en el comando `mode`.
- El texto de la opción en Ajustes pasa de `Solo jefes (si no hay, todo)` a
  `Solo jefes`, en inglés, español y ruso.
- La opción `--boss` de la CLI usa la misma regla.

## Capabilities

### New Capabilities

- `boss-target-mode`: qué daño mide el medidor en cada modo, cuándo abre una
  pelea y qué peleas quedan en pantalla y en el historial con el modo `Jefes`.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Core/Combat/CombatTracker.cs`: la regla de `TargetMode.BossOnly`
  en el manejo del daño.
- `src/AionMeter.Core/Combat/MeterOptions.cs`: descripción del modo.
- `src/AionMeter.Core/Text`: texto de la opción `MeasureBoss` en en/es/ru.
- `tests/AionMeter.Tests/EngineTests.cs`: tests nuevos del modo estricto y
  ajuste de los que daban por hecho que sin jefe se medía todo.
- `README.md`, `README.ru.md` y `docs/linux-overlay.md`: descripción del modo.
- Notas de la próxima versión: avisar del cambio del modo predeterminado.
