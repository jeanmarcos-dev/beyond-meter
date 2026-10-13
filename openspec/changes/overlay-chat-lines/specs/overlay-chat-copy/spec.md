## Purpose

Llevar el resultado de una pelea al chat del juego con un solo clic o atajo, sin
que el mensaje se corte por el límite de caracteres del chat.

## ADDED Requirements

### Requirement: La línea copiada cabe en el chat

Toda línea que el overlay copie al portapapeles SHALL tener como máximo 180
caracteres. Cuando la línea de grupo no cabe entera, SHALL omitir jugadores
completos desde el último puesto, nunca cortar un nombre a la mitad.

#### Scenario: Grupo grande con nombres largos

- **WHEN** la pelea tiene ocho jugadores cuyos nombres y cifras suman más de 180
  caracteres
- **THEN** la línea copiada mide 180 caracteres o menos
- **AND** termina en el último jugador que cupo completo

### Requirement: Línea de grupo

El botón "Copiar DPS" y el comando `copy` SHALL copiar una línea con el título de
la pelea, su duración, el resultado si fue KILL o WIPE, y los jugadores por
puesto con su DPS. La fila de invocaciones sin dueño NO SHALL aparecer.

#### Scenario: Copiar una pelea terminada en kill

- **WHEN** se copia una pelea contra un jefe que terminó con su muerte
- **THEN** la línea empieza con el nombre de la pelea, la duración y `KILL`
- **AND** sigue con `1.<nombre> <DPS>` para cada jugador en orden de daño

#### Scenario: Copiar desde el historial

- **WHEN** el overlay muestra una pelea guardada y se copia
- **THEN** la línea corresponde a la pelea mostrada, no a la pelea en vivo

### Requirement: Línea de un jugador

El panel de skills SHALL ofrecer copiar la línea del jugador que muestra: título
de la pelea, puesto, nombre, DPS, porcentaje del daño total y DPS del grupo. El
comando `copyself` SHALL copiar esa misma línea para el personaje propio.

#### Scenario: Copiar a otro jugador

- **WHEN** el panel muestra las skills de un compañero y se pulsa copiar
- **THEN** la línea contiene `#<puesto> <nombre> <DPS>/s (<porcentaje>)` de ese
  compañero y el DPS del grupo

#### Scenario: Copiar la línea propia con el overlay fijado

- **WHEN** el overlay está fijado y se ejecuta `aionmeter-overlay copyself`
- **THEN** el portapapeles recibe la línea del personaje propio

### Requirement: Sin pelea no se copia nada

Cuando no hay pelea que mostrar, copiar NO SHALL modificar el portapapeles.

#### Scenario: Copiar esperando combate

- **WHEN** el overlay muestra "Esperando combate" y se ejecuta `copy`
- **THEN** el contenido previo del portapapeles sigue intacto

### Requirement: Confirmación visible

Tras copiar desde un botón, ese botón SHALL indicar durante un momento que la
copia se hizo y luego volver a su texto original.

#### Scenario: Pulsar Copiar DPS

- **WHEN** se pulsa "Copiar DPS" con una pelea en pantalla
- **THEN** el botón muestra "Copiado ✓" y vuelve a "Copiar DPS" poco después
