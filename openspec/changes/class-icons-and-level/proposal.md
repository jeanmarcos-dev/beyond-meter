## Why

En el overlay cada jugador se distingue solo por el nombre y el color de la
barra: con un grupo grande no se sabe de un vistazo quién es el clérigo o el
gladiador, y el color solo no alcanza. La app de Windows del upstream ya trae
los íconos de las 9 clases y el Core ya conoce la clase de cada jugador. El
paquete que identifica al propio personaje trae además su nivel, que el
parser ya lee pero no llega a ninguna vista.

## What Changes

- Cada fila del overlay muestra el ícono de la clase del jugador a la
  izquierda del nombre; sin clase conocida, la fila queda sin ícono.
- El encabezado del detalle de skills muestra el ícono de la clase.
- El nivel del propio personaje viaja del paquete de identidad hasta la fila y
  el detalle, como "Nv 45". El de los demás jugadores no se muestra hasta
  confirmar en una captura real dónde viene en su paquete.

## Capabilities

### New Capabilities

- `class-icons-and-level`: ícono de clase y nivel del jugador en las filas y
  el detalle del overlay.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Core`: `PlayerInfo.Level`, `EntityRegistry.SetSelf` con nivel,
  `Combatant.Level`, campo opcional `Level` al final de `CombatantSnapshot` y de
  `CombatantDetail` (compatible con el JSON del historial y con la app de
  Windows).
- `src/AionMeter.Overlay`: los PNG de `src/AionMeter.App/Assets/Classes` se
  enlazan como recursos de Avalonia; `ClassIcons`, `RowView`, `DetailView`,
  `RowControl` y `SkillPanel`.
