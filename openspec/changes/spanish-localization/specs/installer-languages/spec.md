## Purpose

Definir en qué idiomas habla el instalador de Windows y las instrucciones que se
entregan con la app, para que un jugador que habla español pueda instalarla sin
leer otro idioma.

## ADDED Requirements

### Requirement: Instalador en español

El instalador SHALL ofrecer el español además del inglés y el ruso. En español,
SHALL mostrar en español tanto los textos propios del asistente como los
mensajes propios de Beyond Meter: la página de Npcap, el aviso de cerrar la app
abierta y la pregunta de borrar ajustes e historial al desinstalar.

#### Scenario: Instalar en español

- **WHEN** el jugador elige español al iniciar el instalador
- **THEN** el asistente y la página de Npcap están en español

#### Scenario: Desinstalar en español

- **WHEN** Beyond Meter se instaló en español y el jugador lo desinstala
- **THEN** la pregunta de borrar ajustes, historial y timers está en español

#### Scenario: Actualización silenciosa

- **WHEN** la app se actualiza sola con el instalador
- **THEN** la instalación termina sin mostrar diálogos de idioma ni otros mensajes

### Requirement: Instrucciones en español

`INSTALL.txt` SHALL incluir una sección en español con los mismos pasos que las
secciones en inglés y ruso: requisitos, instalador, zip portátil, actualización,
desinstalación y aviso de uso.

#### Scenario: Leer las instrucciones

- **WHEN** el jugador abre `INSTALL.txt` del zip portátil
- **THEN** encuentra una sección "ESPAÑOL" con los mismos pasos que la sección en inglés
