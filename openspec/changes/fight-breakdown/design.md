# Design

## Context

`CombatantDetail` trae `PerSecond` (daño por segundo de pelea), `Casts`
(`CastRecord(OffsetMs, SkillCode, Damage, Flags)`, hasta 20 000 por jugador),
`Quality`, `Skills`, `DamageTaken` y `HitsTaken`. En vivo se obtiene con
`CombatTracker.Detail`; en una pelea guardada viene en `FightRecord.Details`.
`MeterSession.Detail` ya resuelve cuál usar. El overlay es una ventana sin
bordes, siempre encima y con entrada transparente al fijarla: no sirve para una
vista de análisis.

## Goals / Non-Goals

**Goals:**
- Paridad funcional con el desglose de Windows en las cuatro vistas.
- Lógica de series y rotación testeable sin UI.

**Non-Goals:**
- Copiar el desglose como imagen.
- Iconos de skills (descarga de un CDN de terceros).
- Comparar dos peleas.

## Decisions

- **Ventana Avalonia normal** abierta por el mismo proceso del overlay, para
  compartir `MeterSession` y no duplicar la captura. Alternativa descartada:
  un proceso aparte leyendo el historial, que no vería la pelea en vivo.
- **Cálculo separado del dibujo**: `BreakdownView` (puro) produce las series
  suavizadas, la escala del eje, las filas de skills y los elementos de
  rotación ya dimensionados; los controles solo dibujan. Los escenarios del
  spec se testean contra `BreakdownView`.
- **Gráficos con `DrawingContext`** (`StreamGeometry` para las curvas) en un
  control propio. Alternativa descartada: LiveCharts/ScottPlot, una
  dependencia pesada para dos gráficos.
- **Rotación virtualizada por ventana de tiempo**: se dibujan solo los
  lanzamientos visibles con scroll horizontal, porque 20 000 controles
  bloquearían la UI.
- **Selección compartida**: abrir el desglose desde un jugador usa la misma
  pelea (`SelectedFight`) del overlay en ese momento y la congela en la
  ventana; navegar después en el overlay no cambia una ventana ya abierta.

## Risks / Trade-offs

- [Refrescar el detalle completo cada segundo copia hasta 20 000
  lanzamientos] → en vivo se pide el detalle una vez por segundo, no cada
  250 ms, y solo del jugador seleccionado.
- [XWayland a escala 2 puede abrir la ventana fuera de pantalla] → se abre
  centrada sobre el monitor del overlay y se recuerda su tamaño.
