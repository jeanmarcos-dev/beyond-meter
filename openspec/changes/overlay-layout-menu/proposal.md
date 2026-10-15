## Why

El overlay muestra siempre lo mismo: encabezado completo, cuatro columnas y doce
botones. Quien solo quiere su DPS y el ping no tiene cómo despejarlo, y para
cambiar el ping o la barra del jefe hay que abrir la ventana de Ajustes. Además
el ancho solo se cambia desde un pequeño `⇔` en la esquina y la cantidad de
filas solo desde Ajustes, cuando lo esperable es arrastrar el borde de la
ventana.

## What Changes

- Un botón `⚙` en el encabezado, junto a la `✕`, que abre un menú con casillas
  para mostrar u ocultar cada parte del overlay: ping, reloj de la pelea, línea
  de total y grupo, estado (`En vivo`, historial, repetición), barra de vida del
  jefe, línea del próximo jefe, cada columna de la tabla (DPS, Daño, %, Crít),
  cada botón de la barra y el pie de atajos. Al final, `Más ajustes…` abre la
  ventana de Ajustes de siempre.
- El `⚙` de la barra de botones se quita: su lugar pasa al encabezado.
- Las partes ocultas se guardan en `settings.json` y se respetan al reiniciar.
  El ping y la barra del jefe siguen usando sus preferencias de siempre
  (`ShowPing`, `ShowBossPanel`), así que el menú y Ajustes muestran lo mismo.
- Redimensionado por los bordes: los bordes izquierdo y derecho cambian el
  ancho; el borde inferior cambia cuántas filas de jugadores se ven, de fila en
  fila; las esquinas inferiores hacen las dos cosas. El cursor cambia sobre
  cada borde. La altura sigue ajustándose al contenido. El `⇔` se quita.
- Con el overlay fijado no hay menú ni redimensionado, como hoy con los botones.
- Textos nuevos en inglés, español y ruso.

## Capabilities

### New Capabilities

- `overlay-layout`: elegir desde el overlay qué partes se ven y cambiar su
  tamaño arrastrando los bordes.

### Modified Capabilities

(ninguna)

## Impact

- `src/AionMeter.Core/Settings`: enum de partes del overlay y lista
  `HiddenParts` en `UserSettings`, con su lectura tolerante.
- `src/AionMeter.Overlay/OverlayWindow.cs`: botón `⚙` en el encabezado, menú de
  partes, visibilidad de cada parte, bordes de redimensionado; se quitan el
  `⇔` y el `⚙` de la barra.
- `src/AionMeter.Overlay/RowControl.cs`: columnas que se pueden ocultar.
- Nueva lógica pura de bordes (qué borde hay bajo el puntero y el tamaño
  resultante) con unit tests.
- `src/AionMeter.Core/Text`: cadenas en en/es/ru.
- `docs/linux-overlay.md` y `README`s: el menú, los bordes y la clave nueva.
