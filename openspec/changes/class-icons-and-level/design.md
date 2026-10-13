# Design

## Context

`PacketParser.ParseIdentity` emite `SelfIdentifiedEvent` con `Level` cuando el
servidor y la clase validan; los demás jugadores llegan como `PlayerSeenEvent`
sin nivel. `CombatTracker` pasa la clase a `EntityRegistry` y de ahí a
`Combatant` en `RefreshIdentity`, pero descarta el nivel. El overlay usa la
clase solo para el color (`ClassColors`). Los íconos están en
`src/AionMeter.App/Assets/Classes/*.png` (40x42, salvo brawler 128x128).

## Goals / Non-Goals

**Goals:**
- Ícono de clase en filas y detalle, sin duplicar los PNG en el repo.
- Nivel del propio personaje de punta a punta.

**Non-Goals:**
- Nivel de los demás jugadores: requiere ubicarlo en su paquete con una
  captura real; queda para un change posterior.
- Íconos en la ventana de desglose y en las líneas de chat.

## Decisions

- **Recursos enlazados**: `AvaloniaResource` con `Link` hacia los PNG de la app
  de Windows, para que ambos compartan los mismos archivos.
- **`ClassIcons.For(GameClass)`** carga cada bitmap una vez desde
  `avares://aionmeter-overlay/Assets/Classes/` y devuelve `null` para
  `Unknown`; un ícono faltante no rompe la fila.
- **Columna fija de 18 px para el ícono**: los nombres quedan alineados haya o
  no ícono.
- **Nivel en `PlayerInfo`** solo se actualiza con valores mayores que 0, igual
  que la clase, para que un paquete sin nivel no lo borre.
- **`Level = 0` al final de `CombatantSnapshot` y `CombatantDetail`**: el JSON
  viejo carga con 0 y la app de Windows compila sin cambios.
- **El texto "Nv N" se arma en `MeterView`** y llega como `string?` a la vista.

## Risks / Trade-offs

- [El nivel del paquete propio podría no ser el nivel real en alguna región] →
  el parser ya lo acota a 1-99; se confirma en el juego en la verificación
  final.
