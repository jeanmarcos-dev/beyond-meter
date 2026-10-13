## Purpose

Definir cómo se integra la app con el escritorio de cada sistema (bandeja,
atajos, click-through, notificaciones, instancia única y comandos externos), para
que el jugador la controle igual en Windows y Linux mientras juega.

## ADDED Requirements

### Requirement: Icono en la bandeja

La app SHALL mostrar un icono en el área de notificación, con un menú para
mostrar u ocultar el overlay, activar el click-through, reiniciar el medidor,
abrir el historial, los timers y los Ajustes, cambiar el idioma, iniciar una
demo o un replay, buscar actualizaciones y salir. En Linux, si el escritorio no
tiene área de notificación, la app SHALL seguir funcionando sin el icono.

#### Scenario: Ocultar desde la bandeja

- **WHEN** el jugador elige "Ocultar el overlay" en el menú de la bandeja
- **THEN** el overlay se oculta y el elemento del menú pasa a "Mostrar el overlay"

### Requirement: Atajos globales

En Windows la app SHALL registrar atajos globales configurables para mostrar u
ocultar el overlay, reiniciar el medidor, el click-through y los timers, con los
mismos valores por defecto que la app WPF. Los atajos SHALL funcionar con el
juego en primer plano y no SHALL leer el resto del teclado. En Linux los mismos
comandos SHALL seguir disponibles para asignarlos a atajos del escritorio.

#### Scenario: Atajo con el juego en primer plano

- **WHEN** el juego tiene el foco en Windows y el jugador pulsa Ctrl+Shift+D
- **THEN** el overlay se muestra o se oculta

#### Scenario: Atajo ocupado

- **WHEN** otro programa ya registró el atajo configurado
- **THEN** la app avisa que ese atajo no está disponible y los demás funcionan

### Requirement: Click-through

Con el overlay bloqueado, los clics SHALL atravesarlo hacia el juego en Windows y
en Linux. Desbloqueado, SHALL recibir clics.

#### Scenario: Bloquear en Windows

- **WHEN** el jugador bloquea el overlay en Windows y hace clic sobre él
- **THEN** el clic llega al juego

### Requirement: Notificaciones

Las alertas de jefes y los avisos de actualización SHALL mostrarse como
notificación del sistema, con sonido en las alertas: en Windows, notificaciones
de Windows; en Linux, notificaciones del escritorio. Si no hay notificaciones
disponibles, la app SHALL seguir funcionando.

#### Scenario: Alerta de jefe en Windows

- **WHEN** un jefe vigilado está por reaparecer y la app corre en Windows
- **THEN** Windows muestra una notificación con el nombre del jefe y suena una alerta

### Requirement: Instancia única y comandos

SHALL correr una sola copia de la app por usuario. Abrirla de nuevo SHALL mostrar
la copia que ya corre. Los comandos de línea (`show`, `toggle`, `lock`, `quit`,
etc.) SHALL enviarse a la copia que corre, en Windows y en Linux.

#### Scenario: Abrir dos veces

- **WHEN** la app ya corre oculta y el jugador abre el acceso directo
- **THEN** no arranca una segunda copia y el overlay se muestra
