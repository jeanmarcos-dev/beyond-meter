## Purpose

Que el overlay esté en pantalla cuando aporta (jugando y en combate) y fuera de
ella cuando no, sin que el jugador tenga que abrirlo, cerrarlo ni ocultarlo a
mano, y sin dejar de medir.

## ADDED Requirements

### Requirement: Arranque con la sesión

El proyecto SHALL incluir una unidad de usuario de systemd que inicie el
overlay al iniciar la sesión gráfica y lo reinicie si termina con error. Un
cierre pedido por el jugador (`quit` o ✕) NO SHALL provocar reinicio.

#### Scenario: Iniciar sesión

- **WHEN** el jugador inicia sesión con la unidad habilitada
- **THEN** el proceso del overlay está corriendo, oculto

#### Scenario: Caída del overlay

- **WHEN** el proceso del overlay termina por una excepción
- **THEN** systemd lo vuelve a iniciar

#### Scenario: Cerrar con la ✕

- **WHEN** el jugador cierra el overlay con la ✕
- **THEN** el overlay no vuelve a abrirse hasta el próximo inicio de sesión o
  hasta que se lance a mano

### Requirement: Visibilidad según el juego

Con el juego cerrado, el overlay SHALL estar oculto. Al detectar el juego, SHALL
mostrarse; al cerrarse el juego, SHALL ocultarse.

#### Scenario: Abrir el juego

- **WHEN** el overlay está oculto porque el juego estaba cerrado y el juego
  arranca
- **THEN** el overlay se muestra

#### Scenario: Cerrar el juego

- **WHEN** el juego se cierra
- **THEN** el overlay se oculta y el proceso sigue corriendo

### Requirement: Auto-ocultar y auto-mostrar por combate

Con el juego abierto, el overlay SHALL ocultarse tras el tiempo configurado sin
combate (por defecto 60 s; 0 desactiva el auto-ocultar) y SHALL mostrarse al
empezar una pelea. Mientras el overlay está sin fijar, NO SHALL auto-ocultarse.

#### Scenario: Fuera de combate

- **WHEN** termina una pelea y pasan 60 s sin otra
- **THEN** el overlay se oculta

#### Scenario: Vuelve el combate

- **WHEN** el overlay se auto-ocultó y empieza una pelea
- **THEN** el overlay se muestra con la pelea nueva

#### Scenario: Moviendo el overlay

- **WHEN** el overlay está sin fijar y pasan 60 s sin combate
- **THEN** el overlay sigue visible

### Requirement: Ocultar a mano manda

Si el jugador oculta el overlay con `toggle`, el auto-mostrar NO SHALL volver a
mostrarlo hasta que el jugador lo muestre a mano o el juego se cierre y vuelva
a abrirse.

#### Scenario: Ocultar durante una pelea

- **WHEN** el jugador oculta el overlay con `toggle` y empieza otra pelea
- **THEN** el overlay sigue oculto

#### Scenario: Reiniciar el juego tras ocultarlo

- **WHEN** el jugador ocultó el overlay a mano, cierra el juego y lo vuelve a
  abrir
- **THEN** el overlay se muestra

### Requirement: Medir con el overlay oculto

Con el overlay oculto por cualquier motivo, la captura, el cierre de peleas y
el guardado en el historial SHALL seguir funcionando igual.

#### Scenario: Pelea con el overlay oculto a mano

- **WHEN** el overlay está oculto a mano y termina una pelea de jefe
- **THEN** la pelea aparece en el historial
