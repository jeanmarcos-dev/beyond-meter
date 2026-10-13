## Purpose

Que el aporte de los sanadores se vea en el medidor con la misma claridad que el
daño, en vivo y en el historial.

## ADDED Requirements

### Requirement: Los sanadores participan en la pelea

Un jugador que cura a un aliado mientras hay una pelea activa SHALL contar como
participante de esa pelea aunque no haya hecho daño. La curación fuera de una
pelea activa NO SHALL abrir una pelea.

#### Scenario: Clérigo que solo cura

- **WHEN** durante una pelea un clérigo solo lanza curaciones sobre el grupo
- **THEN** el clérigo aparece en la pelea con daño 0 y su curación total

#### Scenario: Curarse fuera de combate

- **WHEN** no hay pelea activa y un jugador se cura
- **THEN** no se abre ninguna pelea

### Requirement: Curación por jugador

La curación de cada jugador SHALL acumularse por pelea, incluir la de sus
invocaciones, y exponerse junto a su curación por segundo y su porcentaje sobre
la curación total del grupo. La curación por segundo SHALL usar la misma
duración de pelea que el DPS.

#### Scenario: Curación de una invocación

- **WHEN** la invocación de un jugador cura a un aliado
- **THEN** esa curación suma al total del jugador dueño

### Requirement: Curación en el historial

Las peleas guardadas SHALL conservar la curación de cada jugador. Una pelea
guardada antes de que existiera este dato SHALL abrirse sin errores, con
curación 0.

#### Scenario: Pelea vieja

- **WHEN** se abre una pelea guardada sin datos de curación
- **THEN** la vista de curación muestra a todos con 0

### Requirement: Vista de curación en el overlay

El overlay SHALL alternar entre las vistas "Daño" y "Curación" con un botón en
la barra y con el comando `heal`. La vista de curación SHALL ordenar por
curación y mostrar puesto, curación por segundo, total y porcentaje del grupo,
con las mismas barras de color de clase. Los jugadores sin curación NO SHALL
aparecer en esa vista. La vista elegida SHALL mantenerse al navegar el
historial.

#### Scenario: Cambiar a curación

- **WHEN** se ejecuta `heal` durante una pelea con un clérigo y un cantor
- **THEN** la tabla muestra solo a quienes curaron, ordenados por curación

#### Scenario: Volver a daño

- **WHEN** la vista es "Curación" y se ejecuta `heal`
- **THEN** la tabla vuelve a la vista de daño
