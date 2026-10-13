## Why

En un grupo de AION 2 el clérigo y el cantor sostienen la pelea, y el medidor
solo les ve el poco daño que hacen: quedan últimos en la tabla aunque hayan
hecho el trabajo más importante. El Core ya decodifica las curaciones del
tráfico y las suma por jugador, pero ese total no llega a ninguna vista ni al
historial, y un sanador que no hace daño ni siquiera entra en la pelea.

## What Changes

- El Core cuenta como participante de la pelea a quien cura a un aliado
  durante ella, aunque no haga daño.
- La curación total de cada jugador entra en las instantáneas de la pelea y en
  las peleas guardadas.
- El overlay gana una vista "Curación" (botón en la barra y comando `heal`)
  con el mismo formato de la tabla de daño: puesto, curación por segundo, total
  y porcentaje del grupo.
- Las peleas guardadas antes de este cambio se abren igual, con curación 0.

## Capabilities

### New Capabilities

- `healing-meter`: medición, persistencia y visualización de la curación por
  jugador en cada pelea.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Core`: `CombatTracker.OnHeal` (alta de sanadores),
  `CombatantSnapshot` (campo de curación con valor por defecto, compatible con
  el JSON viejo), cálculo de HPS en `BuildSnapshot`.
- La app de Windows recibe el campo nuevo sin cambios de comportamiento.
- `src/AionMeter.Overlay`: modo de vista daño/curación en `MeterView`,
  `OverlayWindow` y `OverlayCommand`.
- Cambio de Core candidato a PR upstream.
