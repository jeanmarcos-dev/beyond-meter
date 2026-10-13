## Purpose

Permitir que el jugador ajuste cómo mide, guarda y se ve el overlay de Linux, y
que esos ajustes sobrevivan a reinicios sin que un archivo mal editado lo rompa.

## ADDED Requirements

### Requirement: Modo de objetivo

El overlay SHALL ofrecer dos modos de objetivo: "Jefes", que solo cuenta el daño
a jefes, y "Todo", que cuenta todo lo que el jugador golpea. El modo SHALL
poder alternarse desde la barra con el overlay sin fijar y con el comando
`mode`, SHALL verse en la barra y SHALL persistir entre reinicios. Por defecto
SHALL ser "Jefes".

#### Scenario: Pegarle a un mob en modo Todo

- **WHEN** el modo es "Todo" y el jugador golpea a un mob que no es jefe
- **THEN** la tabla muestra una pelea con ese daño

#### Scenario: Pegarle a un mob en modo Jefes

- **WHEN** el modo es "Jefes" y el jugador golpea solo a mobs que no son jefes
- **THEN** la tabla sigue en "Esperando combate"

#### Scenario: El modo sobrevive a un reinicio

- **WHEN** se cambia a "Todo", se cierra el overlay y se vuelve a abrir
- **THEN** el overlay arranca en modo "Todo"

### Requirement: Idioma de los nombres

El overlay SHALL mostrar los nombres de skills y NPC en el idioma configurado,
elegido entre los que trae la carpeta de datos. Un nombre que falte en ese
idioma SHALL mostrarse en inglés. Por defecto el idioma SHALL ser inglés.

#### Scenario: Nombres en español

- **WHEN** el idioma configurado es `es`
- **THEN** las skills del panel aparecen con su nombre en español

#### Scenario: Idioma inexistente

- **WHEN** el idioma configurado no existe en la carpeta de datos
- **THEN** el overlay arranca con nombres en inglés

### Requirement: Cierre de pelea por inactividad

El overlay SHALL cerrar una pelea tras un tiempo configurable sin daño: uno
para peleas normales (por defecto 10 s, entre 3 y 120) y otro mientras hay un
jefe vivo (por defecto 30 s, entre 5 y 300).

#### Scenario: Pausa larga contra un jefe

- **WHEN** hay un jefe vivo y pasan 20 s sin daño con el valor por defecto
- **THEN** la pelea sigue abierta

### Requirement: Qué peleas se guardan

El overlay SHALL guardar en el historial según el modo configurado: solo
jefes, jefes y peleas que duren al menos N segundos, o todas. Por defecto SHALL
ser "jefes y peleas de al menos 20 s". Una pelea de menos de 5 s NO SHALL
guardarse en ningún modo.

#### Scenario: Mob suelto con el valor por defecto

- **WHEN** termina una pelea de 12 s contra un mob que no es jefe
- **THEN** no aparece en el historial

#### Scenario: Jefe corto con el valor por defecto

- **WHEN** termina una pelea de 8 s contra un jefe
- **THEN** aparece en el historial

### Requirement: Opacidad del overlay fijado

El fondo del overlay fijado SHALL usar la opacidad configurada, entre 0,15 y 1,
con 0,55 por defecto. Sin fijar, el fondo SHALL seguir siendo casi opaco para
poder leer los botones.

#### Scenario: Overlay más transparente

- **WHEN** la opacidad configurada es 0,3 y el overlay está fijado
- **THEN** el fondo se dibuja con 30 % de opacidad

### Requirement: Archivo de preferencias tolerante

Un valor fuera de rango SHALL ajustarse al límite más cercano. Un valor
ilegible, de tipo equivocado o un archivo corrupto SHALL reemplazarse por el
valor por defecto sin impedir el arranque ni borrar las demás preferencias
legibles.

#### Scenario: Opacidad fuera de rango

- **WHEN** el archivo dice opacidad 5
- **THEN** el overlay usa opacidad 1

#### Scenario: Un valor ilegible no arrastra a los demás

- **WHEN** el modo de historial tiene un texto desconocido y la posición es válida
- **THEN** el modo de historial toma su valor por defecto
- **AND** el overlay abre en la posición guardada

### Requirement: Aplicar sin reiniciar

El comando `reload` SHALL releer el archivo de preferencias y aplicar modo,
idioma, tiempos, política de guardado y opacidad sin cerrar el overlay ni
perder la pelea en curso.

#### Scenario: Cambiar el idioma en caliente

- **WHEN** se edita el idioma a `es` en el archivo y se ejecuta `reload`
- **THEN** las skills del panel pasan a español
- **AND** la pelea en curso conserva su daño acumulado
