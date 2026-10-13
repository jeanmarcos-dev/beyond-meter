## Purpose

Reconocer a cada jugador del grupo por su clase y ver el nivel del propio
personaje sin salir del overlay.

## ADDED Requirements

### Requirement: Ícono de clase en las filas

Cada fila del overlay SHALL mostrar el ícono de la clase del jugador a la
izquierda del nombre, en la vista de daño y en la de curación, en vivo y en el
historial. Una fila sin clase conocida, incluida la de invocaciones sin dueño,
SHALL mostrarse sin ícono y con el nombre en la misma posición.

#### Scenario: Grupo con clases conocidas

- **WHEN** en la pelea participan un gladiador y un clérigo
- **THEN** la fila del gladiador muestra el ícono de gladiador y la del clérigo el de clérigo

#### Scenario: Clase desconocida

- **WHEN** un jugador aún no tiene clase identificada
- **THEN** su fila no muestra ícono

### Requirement: Ícono de clase en el detalle

El encabezado del detalle de skills SHALL mostrar el ícono de la clase del
jugador seleccionado junto al título.

#### Scenario: Abrir el detalle de un cantor

- **WHEN** se abre el detalle de skills de un cantor
- **THEN** el encabezado muestra el ícono de cantor

### Requirement: Nivel del propio personaje

El nivel que trae el paquete de identidad del propio personaje SHALL llegar a
la instantánea de la pelea y mostrarse como "Nv N" en su fila y en el
encabezado de su detalle. Sin nivel conocido (0) NO SHALL mostrarse nada. Las
peleas guardadas antes de este cambio SHALL abrirse sin errores y sin nivel.

#### Scenario: Personaje identificado con nivel

- **WHEN** el juego identifica al propio personaje con nivel 45
- **THEN** su fila muestra "Nv 45"

#### Scenario: Pelea vieja

- **WHEN** se abre una pelea guardada sin el dato de nivel
- **THEN** ninguna fila muestra nivel
