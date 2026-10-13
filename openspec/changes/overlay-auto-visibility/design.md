# Design

## Context

`LiveCapture` informa por `CaptureStatus` si el juego está abierto
(`WaitingForGame` con "AION 2 is not running") y `CombatTracker.LiveFight()`
dice si hay pelea activa. `OverlayWindow.SetVisible` ya muestra y oculta sin
cerrar el proceso. `toggle` arranca una instancia si no hay ninguna.

## Goals / Non-Goals

**Goals:**
- Reglas de visibilidad en una máquina de estados pura y testeable.
- Que el overlay funcione igual lanzado a mano o por systemd.

**Non-Goals:**
- Lanzar el overlay desde las opciones de lanzamiento de Steam.
- Detectar si el juego tiene el foco.

## Decisions

- **`VisibilityPolicy` pura** con entradas (juego abierto, pelea activa,
  segundos sin combate, bloqueado, orden manual) y salida (visible u oculto).
  El temporizador de 250 ms le pasa el estado y la ventana aplica el resultado.
  Los escenarios del spec se testean contra la política, sin UI.
- **Ocultar a mano como estado**, no como bandera persistida: se limpia al
  pasar el juego de abierto a cerrado. No se guarda en `overlay.json` porque
  sobrevivir a un reinicio contradiría "se muestra al reabrir el juego".
- **Unidad systemd** con `PartOf=graphical-session.target`,
  `After=graphical-session.target`, `Restart=on-failure` y
  `ExecStart=%h/.local/bin/aionmeter-overlay show --hidden`. Un `quit` termina
  con código 0, así que `on-failure` no lo reinicia. Alternativa descartada:
  autostart `.desktop`, que no reinicia tras una caída.
- **Flag `--hidden`** para arrancar sin mostrar la ventana; sin él, lanzar a
  mano conserva el comportamiento actual.
- **`DISPLAY` en la unidad**: se hereda de la sesión de GNOME vía
  `systemctl --user import-environment`, que GNOME ya hace; la unidad no lo
  fija a mano.

## Risks / Trade-offs

- [Mostrar la ventana de golpe roba el foco al juego] → la ventana ya usa
  `ShowActivated = false`; se añade un test que lo verifica tras un
  auto-mostrar.
- [Unidad iniciada antes de que XWayland esté listo] → `Restart=on-failure`
  con `RestartSec=5` cubre el arranque temprano.
