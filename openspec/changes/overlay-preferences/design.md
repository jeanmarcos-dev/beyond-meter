# Design

## Context

`OverlaySettings` es un record con posición, ancho, bloqueo y filas, leído con
`System.Text.Json` desde `~/.config/aionmeter/overlay.json`. Si una clave falla,
hoy se pierde el archivo entero y se vuelve a los valores por defecto.
`CombatTracker` lee `MeterOptions` en cada evento, así que cambiar sus
propiedades en caliente ya funciona. `GameData.ReloadNames` cambia el idioma de
los nombres sin recargar el resto.

## Goals / Non-Goals

**Goals:**
- Un solo archivo de preferencias, legible y editable a mano.
- Que ningún valor malo impida arrancar o arrastre a los demás.

**Non-Goals:**
- Ventana de ajustes. Solo el modo de objetivo tiene control en la barra; el
  resto se edita en el archivo y se aplica con `reload`.
- Cambiar el idioma de la UI del overlay, que sigue en español.

## Decisions

- **Lectura por clave en vez de deserializar el record.** Se parsea con
  `JsonDocument` y cada propiedad se lee y valida por separado contra su
  default y su rango. Alternativa descartada: deserializar y validar después,
  porque un solo tipo equivocado tira el archivo completo (el bug actual).
- **Validación en una función pura** `OverlaySettings.Parse(string json)` que
  devuelve el record ya acotado. Es lo que testean los escenarios de rango y
  tolerancia, sin tocar disco.
- **Política de guardado en `MeterSession`**, recibida como valor
  (`HistoryPolicy`) en lugar del `minSavedFightMs` actual. Replica la lógica de
  `MeterService.ShouldSave` de la app de Windows; no se mueve al Core para no
  tocar la app de Windows en este change.
- **`reload` aplica sobre los objetos vivos**: muta `MeterOptions`, llama a
  `GameData.ReloadNames`, reemplaza la política de la sesión y repinta. No se
  recrea el tracker, por eso la pelea en curso sobrevive.
- **El botón Jefes/Todo escribe el archivo al instante**, igual que fijar, para
  que el modo sobreviva a un cierre inesperado.

## Risks / Trade-offs

- [Cambiar a "Todo" en mitad de una pelea de jefe mezcla criterios en esa
  pelea] → el cambio de modo cierra la pelea en curso como `Manual` antes de
  aplicarse, igual que un reset.
- [El nuevo valor por defecto deja de guardar mobs cortos que hoy sí se
  guardan] → se documenta en el README y el modo "todas" sigue disponible.
