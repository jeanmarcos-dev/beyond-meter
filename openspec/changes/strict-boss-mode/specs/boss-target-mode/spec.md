## Purpose

Definir qué daño mide el medidor en cada modo (`Jefes` y `Todo`), cuándo se abre
una pelea y qué peleas quedan en pantalla y en el historial cuando el jugador
solo quiere medir jefes.

## ADDED Requirements

### Requirement: El modo Jefes mide solo jefes

Con el modo `Jefes`, el medidor SHALL contar únicamente el daño que los
jugadores hacen a jefes. Un jefe es un NPC marcado como jefe en los datos del
juego, un muñeco de entrenamiento o, si el NPC no figura en los datos, uno con
5 000 000 de HP máximo o más. El daño a cualquier otro NPC NO SHALL abrir una
pelea, NO SHALL sumarse a la pelea en curso y NO SHALL alargarla, haya o no un
jefe en pelea. El daño recibido y la curación solo SHALL contarse dentro de una
pelea contra un jefe.

#### Scenario: Golpear un mob normal sin jefe en pelea

- **WHEN** el modo es `Jefes`, no hay pelea en curso y el jugador golpea a un
  mob normal
- **THEN** no se abre ninguna pelea y el overlay no cambia

#### Scenario: Golpear a un jefe

- **WHEN** el modo es `Jefes` y el jugador golpea a un jefe
- **THEN** se abre una pelea con ese jefe y su daño se cuenta

#### Scenario: Adds durante la pelea contra un jefe

- **WHEN** el modo es `Jefes`, hay una pelea contra un jefe y el jugador golpea
  a un mob normal
- **THEN** ese daño no se suma a la pelea ni retrasa su cierre por inactividad

#### Scenario: Curación sin jefe en pelea

- **WHEN** el modo es `Jefes`, no hay pelea contra un jefe y un jugador cura a
  otro
- **THEN** no se abre ninguna pelea

### Requirement: El resultado del jefe sigue en pantalla

Con el modo `Jefes`, cuando termina una pelea contra un jefe (muerte, wipe,
inactividad o cambio de zona), el overlay SHALL seguir mostrando ese resultado
aunque el jugador golpee mobs normales. El resultado SHALL cambiar solo cuando
empiece una pelea contra otro jefe o cuando el jugador reinicie el medidor.

#### Scenario: Matar al jefe y seguir con mobs

- **WHEN** el modo es `Jefes`, el jefe muere y luego el jugador pasa varios
  minutos golpeando mobs normales
- **THEN** el overlay sigue mostrando la pelea contra el jefe

#### Scenario: Siguiente jefe

- **WHEN** después de esa pelea el jugador golpea a otro jefe
- **THEN** el overlay pasa a mostrar la pelea nueva

#### Scenario: Reinicio manual

- **WHEN** el jugador reinicia el medidor
- **THEN** el overlay queda en blanco hasta la próxima pelea contra un jefe

### Requirement: El historial del modo Jefes solo tiene jefes

Con el modo `Jefes`, el historial SHALL guardar solo peleas contra jefes,
cualquiera sea la opción `Qué peleas` de Ajustes. Las peleas de menos de 5
segundos NO SHALL guardarse, como hoy. Con el modo `Todo`, la opción
`Qué peleas` SHALL aplicarse como hasta ahora.

#### Scenario: Mobs largos con el modo Jefes

- **WHEN** el modo es `Jefes`, `Qué peleas` es `Todas (de 5 s o más)` y el
  jugador golpea mobs normales durante un minuto
- **THEN** no se guarda ninguna pelea

#### Scenario: Jefe con el modo Jefes

- **WHEN** el modo es `Jefes` y termina una pelea de 30 segundos contra un jefe
- **THEN** la pelea se guarda en el historial

### Requirement: Los mobs no muestran el overlay

Con el modo `Jefes`, golpear mobs normales NO SHALL contar como pelea para
mostrar el overlay de forma automática ni para mantenerlo visible.

#### Scenario: Overlay fijado y oculto mientras se golpean mobs

- **WHEN** el modo es `Jefes`, el overlay está fijado y oculto por inactividad y
  el jugador golpea mobs normales
- **THEN** el overlay sigue oculto

### Requirement: El modo Todo no cambia

Con el modo `Todo`, el medidor SHALL contar el daño a cualquier NPC, como hasta
ahora: golpear un mob normal SHALL abrir una pelea.

#### Scenario: Golpear un mob con el modo Todo

- **WHEN** el modo es `Todo`, no hay pelea en curso y el jugador golpea a un mob
  normal
- **THEN** se abre una pelea con ese mob

### Requirement: Nombre del modo en Ajustes

La opción del modo `Jefes` en Ajustes SHALL llamarse `Solo jefes` en español,
`Boss only` en inglés y `Только боссы` en ruso, sin mencionar que se recurre a
medir todo.

#### Scenario: Ajustes en español

- **WHEN** el jugador abre Ajustes con la interfaz en español
- **THEN** las opciones de `Medir` son `Solo jefes` y `Todo lo que golpeas`
