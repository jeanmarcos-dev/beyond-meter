## Why

El overlay arranca con las opciones por defecto del Core y no deja cambiarlas. La
más visible es el modo de objetivo: hoy solo cuenta el daño a jefes, así que
pegarle a mobs normales no mueve el medidor y parece roto. Tampoco se puede
elegir el idioma de los nombres de skills y NPC (el juego trae diez, incluido
español), cuánto tarda en cerrarse una pelea, qué peleas se guardan ni qué tan
transparente queda el overlay fijado.

## What Changes

- Botón "Jefes / Todo" en la barra y comando `mode` para alternar entre contar
  solo el daño a jefes o todo lo que se golpea. La elección persiste.
- Preferencias nuevas en `~/.config/aionmeter/overlay.json`, con rango y valor
  por defecto documentados:
  - idioma de los nombres de skills y NPC;
  - segundos sin daño para cerrar una pelea, normal y con jefe vivo;
  - qué peleas se guardan: solo jefes, jefes y peleas largas, o todas, más la
    duración mínima de las no-jefe;
  - opacidad del fondo con el overlay fijado.
- Valores fuera de rango se ajustan al límite más cercano; claves desconocidas o
  valores ilegibles vuelven al valor por defecto sin impedir el arranque.
- Comando `reload` para aplicar el archivo editado sin reiniciar.
- **BREAKING**: el historial deja de guardar toda pelea de 5 s o más; el valor
  por defecto pasa a "jefes y peleas de 20 s o más", el mismo de la app de
  Windows, para que el modo "Todo" no llene el historial de mobs sueltos.

## Capabilities

### New Capabilities

- `overlay-preferences`: preferencias persistentes del overlay de Linux, su
  validación y cómo se aplican en caliente.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Overlay`: `OverlaySettings`, `OverlayApp`, `OverlayWindow`,
  `MeterSession` (política de guardado), `OverlayCommand`.
- Usa `MeterOptions`, `GameData.ReloadNames` y `GameData.AvailableLanguages`
  del Core sin modificarlos.
- Los `overlay.json` existentes siguen leyéndose: las claves nuevas toman su
  valor por defecto.
