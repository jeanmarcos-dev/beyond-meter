## Why

"Copiar DPS" arma su propio texto y no respeta el límite de 180 caracteres del chat
del juego: con ocho jugadores y nombres largos el mensaje se corta al pegarlo. El
Core ya trae `ChatLine`, que conoce ese límite y marca KILL/WIPE, y el overlay no
lo usa. Además no hay forma de copiar la línea de un solo jugador, que es lo que
se pega cuando alguien pregunta "¿cuánto hiciste?".

## What Changes

- "Copiar DPS" pasa a copiar la línea de grupo del formato compartido: nunca más
  de 180 caracteres, con el resultado de la pelea (KILL/WIPE) cuando lo hay.
- El panel de skills gana un botón para copiar la línea de ese jugador: puesto,
  DPS, porcentaje y DPS del grupo.
- El comando `copy` sigue copiando la línea de grupo; se añade `copyself` para la
  línea propia, que se puede atar a un atajo y funciona con el overlay fijado.
- El mensaje de uso del binario lista todos los comandos (hoy omite `skills` y
  `copy`).

## Capabilities

### New Capabilities

- `overlay-chat-copy`: copiar al portapapeles el resultado de la pelea mostrada,
  del grupo o de un jugador, en un formato que cabe en el chat del juego.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Overlay`: `MeterView`, `OverlayWindow`, `SkillPanel`,
  `OverlayCommand`, `Program`.
- Desaparece `MeterView.CopyText` en favor de `Core/ChatLine`.
- Sin cambios en el Core ni en la app de Windows.
