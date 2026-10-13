## Why

El panel de skills del overlay responde "qué skill pegó más", pero no "cuándo
bajó mi DPS", "en qué orden lancé" ni "cuánto daño recibí". Para mejorar la
rotación hace falta ver la pelea en el tiempo. El Core ya guarda el daño por
segundo de cada jugador y cada lanzamiento con su daño y si fue crítico, también
en las peleas guardadas, y la app de Windows lo muestra en una ventana de
desglose. En Linux esos datos no se ven.

## What Changes

- Ventana de desglose aparte del overlay, normal (con bordes, redimensionable,
  no siempre encima), que se abre desde el panel de skills con "Ver detalle" o
  con el comando `breakdown`.
- Cuatro pestañas para el jugador elegido, en vivo o desde el historial:
  - **Skills**: la tabla completa (golpes, ticks, daño, DPS, promedio, máximo,
    crítico, espalda, frontal, perfecto, fuerte, multigolpe, porcentaje).
  - **Línea de tiempo**: DPS por segundo del jugador suavizado a 3 s, con el
    resto del grupo en líneas finas de su color de clase.
  - **Rotación**: cada lanzamiento en orden, con tamaño según el daño y marca de
    crítico.
  - **Defensa**: daño recibido, daño recibido por segundo, golpes recibidos.
- Lista del grupo a la izquierda para cambiar de jugador sin cerrar la ventana.
- En vivo se refresca una vez por segundo.

## Capabilities

### New Capabilities

- `fight-breakdown`: análisis detallado de un jugador en una pelea, en vivo o
  guardada: skills completas, DPS en el tiempo, rotación y defensa.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Overlay`: ventana nueva, controles de gráfico y rotación
  dibujados con el render de Avalonia, botón en `SkillPanel`, comando nuevo.
- Lee `CombatantDetail.PerSecond`, `Casts`, `Quality`, `DamageTaken` y
  `HitsTaken`, que el Core ya expone; sin cambios en el Core.
- Sin dependencias nuevas: los gráficos se dibujan a mano, sin librería de
  charts.
