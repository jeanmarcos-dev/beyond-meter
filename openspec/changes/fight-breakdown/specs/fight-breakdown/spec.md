## Purpose

Entender cómo se desarrolló la pelea de un jugador, segundo a segundo y skill a
skill, para mejorar su rotación y su supervivencia.

## ADDED Requirements

### Requirement: Abrir el desglose

El desglose SHALL abrirse en una ventana propia, separada del overlay, desde el
panel de skills de un jugador o con el comando `breakdown`, que abre el del
personaje propio. La ventana SHALL tener bordes, poder moverse y
redimensionarse, y NO SHALL quedar siempre encima del juego.

#### Scenario: Abrir desde el panel de skills

- **WHEN** el panel muestra las skills de un compañero y se pulsa "Ver detalle"
- **THEN** se abre la ventana de desglose con ese compañero seleccionado

#### Scenario: Abrir por comando

- **WHEN** se ejecuta `aionmeter-overlay breakdown` durante una pelea
- **THEN** se abre el desglose del personaje propio en esa pelea

### Requirement: Misma pelea que el overlay

El desglose SHALL mostrar la pelea que el overlay muestra al abrirlo, en vivo o
del historial. En vivo SHALL refrescarse al menos una vez por segundo; una pelea
guardada NO SHALL cambiar.

#### Scenario: Desglose de una pelea guardada

- **WHEN** el overlay navega a una pelea del historial y se abre el desglose
- **THEN** las cifras coinciden con las de esa pelea guardada

### Requirement: Cambiar de jugador

La ventana SHALL listar a todos los jugadores de la pelea por puesto, con su
color de clase y el propio destacado, y SHALL cambiar todas las pestañas al
jugador que se elija.

#### Scenario: Elegir otro jugador

- **WHEN** se elige al tercer puesto en la lista
- **THEN** las cuatro pestañas pasan a mostrar sus datos

### Requirement: Pestaña Skills

La pestaña Skills SHALL mostrar todas las skills del jugador, sin límite de
filas, con golpes, ticks de daño continuo, daño, DPS, promedio, máximo,
porcentaje del daño del jugador y tasas de crítico, espalda, frontal, perfecto,
fuerte y multigolpe. Una tasa sin golpes que medir SHALL mostrarse como "-".

#### Scenario: Skill solo de daño continuo

- **WHEN** una skill solo hizo daño por ticks
- **THEN** sus tasas aparecen como "-"

### Requirement: Línea de tiempo

La pestaña Línea de tiempo SHALL dibujar el DPS por segundo del jugador
suavizado sobre 3 segundos, y el del resto del grupo como líneas de su color de
clase. El eje horizontal SHALL cubrir la duración de la pelea.

#### Scenario: Pausa en mitad de la pelea

- **WHEN** el jugador no hizo daño entre el segundo 20 y el 30
- **THEN** su curva baja en ese tramo

### Requirement: Rotación

La pestaña Rotación SHALL mostrar los lanzamientos del jugador en orden
cronológico, excluyendo los ticks de daño continuo, con tamaño según el daño y
una marca en los críticos. El nombre de la skill y su daño SHALL verse al pasar
el cursor.

#### Scenario: Golpe crítico en la rotación

- **WHEN** un lanzamiento fue crítico
- **THEN** su elemento en la rotación lleva la marca de crítico

### Requirement: Defensa

La pestaña Defensa SHALL mostrar el daño recibido por el jugador, ese daño por
segundo de pelea y la cantidad de golpes recibidos.

#### Scenario: Jugador que tanqueó

- **WHEN** el jefe golpeó 30 veces al jugador por 600K en total en 60 s
- **THEN** la pestaña muestra 600K, 10K por segundo y 30 golpes
