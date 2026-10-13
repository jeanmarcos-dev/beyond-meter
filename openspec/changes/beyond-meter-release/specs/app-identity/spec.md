## Purpose

Define cómo se identifica la app de Windows del fork ante el jugador y ante
Windows, cómo reemplaza al AION2 DPS Meter original en la misma máquina y cómo
da crédito al proyecto del que deriva.

## ADDED Requirements

### Requirement: Nombre visible

La app de Windows SHALL presentarse como "Beyond Meter" en los títulos de sus
ventanas, el ícono de la bandeja y sus globos, los cuadros de mensaje, los
textos de la interfaz en todos sus idiomas, el instalador, los accesos directos
y la entrada de "Aplicaciones instaladas". Ningún texto visible SHALL decir
"AION2 DPS Meter" salvo para referirse al proyecto original.

#### Scenario: Ventana principal

- **WHEN** el jugador abre la app
- **THEN** el título de la ventana del overlay y el tooltip de la bandeja dicen "Beyond Meter"

#### Scenario: Textos en ruso

- **WHEN** la interfaz está en ruso y la app muestra el aviso de Npcap
- **THEN** el aviso nombra a "Beyond Meter"

#### Scenario: Aplicaciones instaladas

- **WHEN** el jugador abre la lista de aplicaciones instaladas de Windows
- **THEN** aparece "Beyond Meter" con la versión instalada

### Requirement: Reemplazo del original

El instalador SHALL tratar una instalación existente del AION2 DPS Meter
original como una versión anterior del mismo programa: SHALL instalar en su
carpeta, SHALL conservar los ajustes, el historial de peleas y los timers de
jefes guardados en `%AppData%\AionMeter`, y al terminar SHALL quedar una sola
entrada en "Aplicaciones instaladas".

#### Scenario: Instalar sobre el original

- **WHEN** el AION2 DPS Meter original está instalado con peleas guardadas y se ejecuta el instalador de Beyond Meter
- **THEN** Beyond Meter queda instalado en la misma carpeta, muestra las peleas guardadas en su historial y la lista de aplicaciones tiene solo "Beyond Meter"

#### Scenario: Instalación limpia

- **WHEN** no hay ninguna versión instalada y se ejecuta el instalador
- **THEN** Beyond Meter se instala para el usuario actual sin pedir permisos de administrador

### Requirement: Sin restos del original

Al instalar sobre el original, el instalador SHALL quitar de la carpeta de
instalación el ejecutable del original y SHALL quitar los accesos directos del
menú Inicio y del escritorio que llevan el nombre del original. Si el
ejecutable del original está abierto desde esa carpeta, el instalador SHALL
cerrarlo antes de quitarlo.

#### Scenario: Accesos directos viejos

- **WHEN** el original tenía acceso directo en el escritorio y en el menú Inicio y se instala Beyond Meter encima
- **THEN** no queda ningún acceso directo "AION2 DPS Meter" y existen los de "Beyond Meter"

#### Scenario: Original abierto durante la instalación

- **WHEN** el original está corriendo desde la carpeta de instalación y se instala Beyond Meter encima
- **THEN** el original se cierra, su ejecutable desaparece de la carpeta y Beyond Meter queda instalado

#### Scenario: Escritorio sin acceso directo

- **WHEN** el jugador había borrado el acceso directo del escritorio del original
- **THEN** la instalación encima no crea uno de Beyond Meter en el escritorio

### Requirement: Crédito al proyecto original

La app de Windows, el README del fork y las notas de cada release SHALL indicar
que Beyond Meter deriva de `cyberbadger6969/aion2-dps-meter` y que se distribuye
bajo GPL-3.0, con enlace al proyecto original.

#### Scenario: README del fork

- **WHEN** alguien abre la página del repositorio del fork
- **THEN** el README nombra el proyecto original con enlace y la licencia GPL-3.0
