## Why

El historial del overlay solo lista peleas y deja abrirlas. Con unas semanas de
juego la lista crece sin control: no hay forma de quedarse con los jefes o con
las peleas ganadas, ni de borrar una pelea de prueba o un wipe que no interesa.
Cada fila tampoco dice lo que más importa al buscar: si se ganó y cómo le fue
al jugador.

## What Changes

- Filtros en el panel de historial: "solo jefes" y "solo kills", combinables,
  con el recuento de peleas mostradas sobre el total.
- Cada fila muestra el resultado (kill, wipe u otro), zona, duración, cantidad
  de jugadores, el DPS propio y el puesto propio.
- Borrar la pelea seleccionada con una confirmación dentro del propio overlay,
  sin diálogos que bloqueen el juego.
- La navegación ◀ ▶ y los comandos `older`/`newer` respetan los filtros
  activos.

## Capabilities

### New Capabilities

- `fight-history`: consulta, filtrado y borrado de las peleas guardadas desde
  el overlay.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Overlay`: `MeterView.History`, `MeterSession` (navegación
  filtrada y borrado), panel de historial de `OverlayWindow`.
- Usa `HistoryStore.List` y `HistoryStore.Delete` del Core, que ya existen.
- Borrar elimina el archivo de la pelea de `~/.local/share/aionmeter/fights`;
  no hay papelera.
