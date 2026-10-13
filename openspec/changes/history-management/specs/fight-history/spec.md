## Purpose

Encontrar rápido una pelea guardada que interesa y deshacerse de las que no,
sin salir del overlay ni interrumpir el juego.

## ADDED Requirements

### Requirement: Filtros del historial

El panel de historial SHALL ofrecer los filtros "solo jefes" y "solo kills",
que se pueden combinar, y SHALL mostrar cuántas peleas cumplen los filtros
sobre el total guardado.

#### Scenario: Solo kills de jefes

- **WHEN** hay 10 peleas guardadas, 4 contra jefes y 3 de ellas kills
- **AND** se activan los dos filtros
- **THEN** la lista muestra esas 3 peleas
- **AND** el recuento dice 3 de 10

### Requirement: Información de cada pelea

Cada fila del historial SHALL mostrar fecha y hora, título, resultado (kill,
wipe u otro), zona, duración, cantidad de jugadores, el DPS del personaje
propio y su puesto. Si el personaje propio no participó, SHALL mostrar el DPS
del grupo y ningún puesto.

#### Scenario: Pelea en la que el propio quedó segundo

- **WHEN** una pelea guardada terminó en kill con el propio en el puesto 2
- **THEN** su fila muestra el símbolo de kill y el puesto 2 junto al DPS propio

### Requirement: Navegación respeta los filtros

La navegación ◀ ▶ y los comandos `older` y `newer` SHALL recorrer solo las
peleas que cumplen los filtros activos.

#### Scenario: Navegar con "solo jefes"

- **WHEN** el filtro "solo jefes" está activo y se pulsa ◀ desde "En vivo"
- **THEN** el overlay muestra la pelea de jefe más reciente
- **AND** se saltan las peleas sin jefe guardadas después de ella

### Requirement: Borrar una pelea

Con una pelea del historial en pantalla, el jugador SHALL poder borrarla. El
borrado SHALL pedir confirmación dentro del overlay, sin abrir diálogos aparte.
Tras confirmar, la pelea SHALL desaparecer del disco y de la lista, y el
overlay SHALL pasar a la pelea siguiente que cumpla los filtros o a "En vivo"
si no queda ninguna.

#### Scenario: Borrar con confirmación

- **WHEN** se pulsa "Borrar" sobre una pelea guardada y luego se confirma
- **THEN** el archivo de esa pelea ya no existe
- **AND** el overlay muestra otra pelea o vuelve a "En vivo"

#### Scenario: Cancelar el borrado

- **WHEN** se pulsa "Borrar" y luego se cancela o pasan 5 segundos sin confirmar
- **THEN** la pelea sigue guardada

#### Scenario: En vivo no se borra

- **WHEN** el overlay muestra la pelea en vivo
- **THEN** la acción de borrar no está disponible
