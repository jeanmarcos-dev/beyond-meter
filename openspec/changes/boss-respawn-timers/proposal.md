## Why

Los jefes de campo de AION 2 reaparecen cada tantos minutos por servidor y
llegar tarde significa perder el loot. El Core ya decodifica todo lo necesario
(la lista de jefes del mapa del juego, avistamientos, muertes) y la app de
Windows lo usa para timers con alerta, pero en Linux esos eventos se descartan.
Es la función de la app original que más uso tiene fuera del combate.

## What Changes

- Los timers de reaparición pasan de la app de Windows al Core, sin cambiar su
  lógica, para que el overlay de Linux los use.
- Panel "Jefes" en el overlay: cada jefe del servidor del personaje con su
  estado (vivo desde, cuenta atrás, debería estar vivo, intervalo desconocido)
  y la hora prevista.
- Marcar un jefe como vigilado, fijar a mano su intervalo, marcarlo muerto o
  quitarlo, con el overlay sin fijar.
- Aviso de escritorio con sonido unos minutos antes de que reaparezca un jefe
  vigilado; los minutos de antelación son una preferencia.
- Comando `timers` para abrir o cerrar el panel; una línea con el próximo jefe
  vigilado queda visible aunque el panel esté cerrado.
- Los timers se guardan en `~/.local/share/aionmeter/` y no los mueven el modo
  demo ni una repetición de captura.

## Capabilities

### New Capabilities

- `boss-respawn-timers`: seguimiento por servidor de la reaparición de jefes de
  campo, su visualización en el overlay y los avisos previos.

### Modified Capabilities

(ninguna)

## Impact

- Se mueve `src/AionMeter.App/Services/BossTimers.cs` a `src/AionMeter.Core`;
  la app de Windows pasa a referenciarlo desde ahí.
- `src/AionMeter.Overlay`: panel nuevo, cableado de `BossNoticed`,
  `FieldBossListed` y `SelfIdentified`, comando `timers`, notificador.
- Dependencia de sistema: `notify-send` y `canberra-gtk-play` (presentes en
  Fedora/GNOME); si faltan, el aviso no se muestra pero el panel funciona.
