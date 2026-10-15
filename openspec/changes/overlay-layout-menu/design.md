## Context

`OverlayWindow` arma el overlay en código (sin XAML): encabezado en un `Grid`,
tabla con un `Grid` de columnas `*,64,64,40,40` repetido en cada `RowControl`,
barra de botones en un `WrapPanel` y un pie con los atajos y el `⇔` que cambia
el ancho. La ventana no tiene decoración (`SystemDecorations.None`,
`CanResize = false`) y su alto sale del contenido (`SizeToContent.Height`). Las
preferencias viven en `UserSettings`, que se lee de forma tolerante campo a
campo. `ShowPing` y `ShowBossPanel` ya existen y tienen casilla en Ajustes.

## Goals / Non-Goals

**Goals:**

- Ocultar y mostrar partes del overlay desde un menú del propio overlay.
- Redimensionar arrastrando los bordes, con el alto medido en filas.
- Lógica de bordes y de partes testeable sin ventana.

**Non-Goals:**

- Alto libre con desplazamiento.
- Reordenar partes o columnas.
- Cambiar la ventana de Ajustes más allá de quitarle el acceso desde la barra.

## Decisions

### Enum `OverlayPart` y lista `HiddenParts`

Un enum en Core con todas las partes del menú y, en `UserSettings`, un conjunto
`HiddenParts` serializado como lista de nombres. `UserSettings.Shows(part)` y
`UserSettings.SetShown(part, bool)` son la única puerta: para `Ping` y
`BossBar` leen y escriben `ShowPing` y `ShowBossPanel`, así el menú y Ajustes
no se contradicen; para el resto usan `HiddenParts`. Se guarda lo oculto y no
lo visible para que una parte nueva en una versión futura aparezca por
defecto.

Alternativa descartada: un booleano por parte. Son veinte claves nuevas en el
archivo y en la lectura, y cada parte nueva obliga a otra.

### `UseSettings` conserva `HiddenParts`

La ventana de Ajustes parte de una copia de las preferencias al abrirse y, al
guardar, las reemplaza todas. Como no edita `HiddenParts`, `UseSettings` copia
el valor vigente igual que ya hace con la posición y el ancho. Así el menú y
Ajustes pueden usarse a la vez.

### Menú con `MenuFlyout` y `StaysOpenOnClick`

El `⚙` lleva un `MenuFlyout` con `MenuItem` de tipo casilla
(`ToggleType = CheckBox`) y `StaysOpenOnClick`, para marcar varias partes sin
reabrir el menú. Columnas y botones van en submenús. Las casillas se
sincronizan con las preferencias al abrir el menú, porque `ShowPing` y
`ShowBossPanel` pueden haber cambiado desde Ajustes.

### Columnas ocultas por ancho cero

Cada `RowControl` y el encabezado de la tabla ponen en 0 el ancho de la columna
oculta y esconden la celda. La columna del nombre (`*`) absorbe el espacio. Se
ocultan por posición, de modo que en la vista de curación la casilla de DPS
oculta HPS.

### Bordes propios en lugar de `BeginResizeDrag`

`BeginResizeDrag` delega en el gestor de ventanas, que cambiaría el alto libre
y choca con `SizeToContent.Height` y con `CanResize = false`. En su lugar, la
ventana mira en qué banda está el puntero (6 px en el margen interior del
marco, que ya tiene 8 px de relleno) con un manejador en túnel, cambia el
cursor y, al pulsar, captura el puntero y calcula el tamaño como hoy hace el
`⇔`. El cálculo vive en una clase pura, `EdgeResize`: dado el borde, el punto
de inicio, el ancho y las filas iniciales, el alto de fila y el desplazamiento,
devuelve ancho, desplazamiento horizontal de la ventana y filas. El borde
superior no redimensiona: ahí está el arrastre para mover la ventana.

Durante el arrastre del borde inferior, las filas sin jugador se muestran
vacías para que la ventana tome el alto final; al soltar vuelven a ocultarse.

### Filas en Ajustes

El combo de filas de Ajustes ofrecía solo 5, 8, 10, 12, 15 y 20 y, al abrirse,
elegía la opción más cercana. Con el borde inferior se puede fijar cualquier
número entre 1 y 20, así que guardar Ajustes habría cambiado, por ejemplo, 7 por
8. El combo agrega el valor vigente a sus opciones cuando no está entre ellas.

## Risks / Trade-offs

- [Mover la ventana al tirar del borde izquierdo puede temblar en X11] → La
  posición y el ancho se fijan en el mismo pase; si tiembla, se puede aplicar
  la posición al soltar. Se comprueba en Linux real.
- [La banda de 6 px es estrecha con escalado alto] → Se mide en DIPs, así que
  escala con la pantalla.
- [Quitar el `⚙` de la barra cambia una costumbre] → El nuevo está siempre a la
  vista en el encabezado; Ajustes también sigue en la bandeja.
