# Design

## Context

`BossTimers` (636 líneas, `src/AionMeter.App/Services/BossTimers.cs`) solo
depende de `System.Text.Json` y del Core; no usa WPF. Contiene la resolución de
slots de la lista del juego, el aprendizaje de intervalos, la persistencia y
`TakeDueAlerts`. `MeterService` lo alimenta con `BossNoticed`,
`FieldBossListed` y `SelfIdentified`, saltándose replay y demo.

## Goals / Non-Goals

**Goals:**
- Reusar la lógica existente sin reescribirla ni cambiar su comportamiento en
  Windows.
- Avisos nativos de GNOME sin dependencias .NET nuevas.

**Non-Goals:**
- Retratos de jefes (requieren descargar de un tercero).
- Sincronizar timers entre jugadores.

## Decisions

- **Mover `BossTimers` a `AionMeter.Core/Timers` y cambiar su namespace.**
  Alternativa descartada: enlazar el archivo desde el proyecto del overlay con
  `<Compile Include>`, que deja el código en un proyecto WPF y complica un PR
  upstream. El movimiento es mecánico; los tests existentes de Windows (si los
  hay) y los nuevos lo cubren.
- **Notificador detrás de una interfaz** `IBossAlert` con una implementación
  que lanza `notify-send --app-name "AION meter" --urgency=critical` y
  `canberra-gtk-play -i alarm-clock-elapsed`. Los tests usan un doble. Si el
  ejecutable no existe, la implementación lo registra una vez y no vuelve a
  intentar.
- **Sondeo de avisos en el temporizador de 250 ms existente**, limitado a una
  consulta por segundo, en vez de un hilo propio.
- **Rutas XDG**: `~/.local/share/aionmeter/boss-timers.json` y
  `field-boss-learned.json`, junto al historial.
- **El panel reemplaza a la tabla mientras está abierto** en vez de apilarse
  debajo, para no duplicar la altura del overlay.

## Risks / Trade-offs

- [El movimiento a Core rompe la compilación de la app de Windows si se olvida
  un `using`] → la app de Windows no compila en Linux; se verifica con
  `dotnet build -p:EnableWindowsTargeting=true` del proyecto App.
- [Los avisos de GNOME no se ven con un juego a pantalla completa exclusiva] →
  el juego corre en ventana sin bordes; además suena el aviso sonoro.

## Migration Plan

Sin migración de datos: en Linux no existían timers. En Windows el archivo y
su formato no cambian.
