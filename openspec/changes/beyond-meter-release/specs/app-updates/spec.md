## Purpose

Asegurar que la app de Windows del fork solo se actualice con releases del
propio fork, para que una versión del proyecto original nunca reemplace a
Beyond Meter sin que el jugador lo decida.

## ADDED Requirements

### Requirement: Origen de las actualizaciones

La app de Windows SHALL buscar versiones nuevas solo en los releases publicados
de `jeanmarcos-dev/beyond-meter`, sin borradores ni pre-releases. Los enlaces
"ver release" y "página de descargas" SHALL apuntar a ese repositorio.

#### Scenario: Hay una versión nueva del fork

- **WHEN** la app está en `1.0.0` y el último release del fork es `v1.1.0`
- **THEN** la app ofrece actualizar a `1.1.0`

#### Scenario: El original publica una versión

- **WHEN** el proyecto original publica un release con número mayor que la versión instalada de Beyond Meter
- **THEN** Beyond Meter no lo ofrece ni lo descarga

### Requirement: Solo instaladores de Beyond Meter

Antes de ejecutar un instalador descargado, la app SHALL comprobar que su nombre
de producto es "Beyond Meter" y SHALL rechazarlo con un error visible si no lo
es, sin cerrar la app.

#### Scenario: Instalador propio

- **WHEN** la actualización descarga el instalador de un release del fork
- **THEN** la app se cierra, el instalador corre en silencio y Beyond Meter vuelve a abrirse en la versión nueva

#### Scenario: Instalador con otro nombre de producto

- **WHEN** el archivo descargado es un instalador cuyo nombre de producto es "AION2 DPS Meter"
- **THEN** la app no lo ejecuta, informa el error y sigue abierta

### Requirement: Comparación de versiones

La app SHALL comparar versiones por mayor, menor y parche, y SHALL ofrecer un
release solo si es estrictamente mayor que la versión en ejecución y el jugador
no eligió omitirlo.

#### Scenario: Misma versión

- **WHEN** la app está en `1.0.0` y el último release del fork es `v1.0.0`
- **THEN** la app informa que está al día
