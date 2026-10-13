## Why

Hoy hay que acordarse de abrir el overlay antes de jugar y de ocultarlo
después. Si no se abre, la pelea no se mide ni se guarda; si se queda abierto
con el juego cerrado, ocupa pantalla en el escritorio. Además, en zonas sin
combate el overlay tapa parte del juego sin aportar nada. El overlay ya sabe si
el juego está abierto y si hay combate; falta que actúe en consecuencia.

## What Changes

- Unidad de usuario de systemd que arranca el overlay con la sesión gráfica y
  lo reinicia si se cae.
- Con el juego cerrado, el overlay permanece oculto; aparece solo cuando el
  juego arranca y se oculta cuando se cierra.
- Auto-ocultar tras N segundos sin combate (por defecto 60, 0 = nunca) y
  auto-mostrar al empezar una pelea.
- Ocultar a mano manda: tras `toggle`, el auto-mostrar no lo vuelve a sacar
  hasta que se muestre a mano o el juego se reinicie.
- Las peleas se siguen midiendo y guardando con el overlay oculto.

## Capabilities

### New Capabilities

- `overlay-visibility`: cuándo el overlay está visible según el estado del
  juego, el combate y las órdenes del jugador, y su arranque con la sesión.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Overlay`: máquina de estados de visibilidad, preferencias de
  auto-ocultar y auto-mostrar.
- Nuevo `packaging/linux/aionmeter-overlay.service` y un script de instalación
  que copia la unidad a `~/.config/systemd/user` y la habilita.
- El binario con `setcap` sigue funcionando bajo systemd de usuario, que no
  activa `NoNewPrivileges` por defecto; la unidad no debe activarlo.
