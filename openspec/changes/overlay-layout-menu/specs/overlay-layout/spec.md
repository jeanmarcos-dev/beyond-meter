## Purpose

Permitir que el jugador decida desde el propio overlay qué partes se ven y que
cambie su tamaño arrastrando los bordes, sin pasar por la ventana de Ajustes.

## ADDED Requirements

### Requirement: Menú de partes en el encabezado

El overlay SHALL mostrar un botón `⚙` en el encabezado, junto al botón de
cerrar. Al pulsarlo SHALL abrir un menú con una casilla por cada parte que se
puede ocultar y, al final, la entrada `Más ajustes…`, que abre la ventana de
Ajustes. La barra de botones NO SHALL tener un botón `⚙`. Con el overlay fijado,
el botón `⚙` NO SHALL verse.

#### Scenario: Abrir el menú

- **WHEN** el jugador pulsa `⚙` con el overlay sin fijar
- **THEN** aparece el menú con las casillas marcadas según lo que se ve

#### Scenario: Más ajustes

- **WHEN** el jugador elige `Más ajustes…`
- **THEN** se abre la ventana de Ajustes

#### Scenario: Overlay fijado

- **WHEN** el overlay está fijado
- **THEN** el botón `⚙` no se ve

### Requirement: Partes que se pueden ocultar

El menú SHALL ofrecer: ping, reloj de la pelea, línea de total y grupo, estado
(en vivo, historial o repetición), barra de vida del jefe, línea del próximo
jefe (solo si hay temporizadores de jefes), un submenú de columnas con DPS,
Daño, % y Crít, un submenú de botones con cada botón de la barra (En vivo,
anterior y siguiente juntos, Historial, Mis habilidades, Reapariciones si hay
temporizadores, Reiniciar, Daño/Curación, Todo/Jefes, Copiar DPS, Fijar e
idioma) y el pie de atajos. Desmarcar una casilla SHALL ocultar esa parte al
instante; marcarla SHALL mostrarla de nuevo. El nombre del jugador NO SHALL
poder ocultarse. En la vista de curación las columnas SHALL ocultarse por su
posición (HPS, Curación, %).

#### Scenario: Ocultar una columna

- **WHEN** el jugador desmarca la columna Crít
- **THEN** la columna desaparece del encabezado de la tabla y de todas las
  filas, y las demás ocupan el espacio

#### Scenario: Ocultar todos los botones

- **WHEN** el jugador desmarca todos los botones de la barra
- **THEN** la barra de botones desaparece, salvo los botones de borrar una
  pelea del historial mientras se recorre el historial

#### Scenario: Volver a mostrar

- **WHEN** el jugador vuelve a marcar el reloj de la pelea
- **THEN** el reloj aparece otra vez en el encabezado

### Requirement: Preferencias de las partes

Las partes ocultas SHALL guardarse en `settings.json` en la clave
`HiddenParts`, como lista de nombres, y respetarse al reiniciar el medidor. Un
nombre desconocido SHALL ignorarse sin afectar al resto. El ping y la barra del
jefe SHALL seguir guardándose en `ShowPing` y `ShowBossPanel`, de modo que el
menú y la ventana de Ajustes muestren el mismo estado. Guardar la ventana de
Ajustes NO SHALL deshacer las partes ocultadas desde el menú.

#### Scenario: Reinicio

- **WHEN** el jugador oculta el reloj y la columna %, y reinicia el medidor
- **THEN** el reloj y la columna % siguen ocultos

#### Scenario: Nombre desconocido

- **WHEN** `HiddenParts` contiene `"Clock"` y `"Algo"`
- **THEN** el reloj queda oculto y lo demás se ve

#### Scenario: Ping desde el menú

- **WHEN** el jugador desmarca el ping en el menú
- **THEN** `ShowPing` queda en falso y la casilla del ping en Ajustes aparece
  desmarcada

### Requirement: Redimensionado por los bordes

Con el overlay sin fijar, arrastrar el borde izquierdo o derecho SHALL cambiar
el ancho entre 280 y 2000; el borde izquierdo SHALL mover también la ventana
para que el borde derecho quede en su sitio. Arrastrar el borde inferior SHALL
cambiar la cantidad de filas de jugadores entre 1 y 20, una fila por cada alto
de fila arrastrado. Las esquinas inferiores SHALL cambiar ancho y filas a la
vez. Sobre cada borde el cursor SHALL indicar la dirección. Al soltar, el ancho,
la posición y las filas SHALL guardarse. Mientras se arrastra el borde
inferior, las filas sin jugador SHALL verse vacías para que la ventana muestre
el alto final. Con el overlay fijado NO SHALL haber redimensionado. El control
`⇔` NO SHALL existir.

#### Scenario: Ensanchar por la derecha

- **WHEN** el jugador arrastra el borde derecho 100 px hacia la derecha
- **THEN** el overlay es 100 px más ancho y la posición no cambia

#### Scenario: Ensanchar por la izquierda

- **WHEN** el jugador arrastra el borde izquierdo 100 px hacia la izquierda
- **THEN** el overlay es 100 px más ancho y su borde derecho no se mueve

#### Scenario: Más filas

- **WHEN** el jugador arrastra el borde inferior hacia abajo el alto de dos
  filas, con 10 filas
- **THEN** el overlay pasa a 12 filas y lo guarda

#### Scenario: Límite de ancho

- **WHEN** el jugador arrastra el borde derecho más allá del mínimo
- **THEN** el ancho se queda en 280

#### Scenario: Fijado

- **WHEN** el overlay está fijado y el jugador arrastra un borde
- **THEN** el tamaño no cambia

### Requirement: Textos traducidos

Las cadenas del menú SHALL existir en inglés, español y ruso.

#### Scenario: Interfaz en español

- **WHEN** el idioma de la interfaz es español
- **THEN** el menú dice `Reloj de la pelea`, `Columnas`, `Botones` y
  `Más ajustes…`
