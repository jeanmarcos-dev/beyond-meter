## Purpose

Definir qué publica cada release del fork, para Windows y para Linux, cómo se
numera y cómo se construye, de modo que un jugador de cualquiera de los dos
sistemas encuentre una descarga que funcione sin compilar nada.

## ADDED Requirements

### Requirement: Numeración propia

Las versiones del fork SHALL seguir `mayor.menor.parche` empezando en `1.0.0`,
independientes de la numeración del original. La app de Windows y el overlay de
Linux de un mismo release SHALL llevar el mismo número, y el tag del release
SHALL ser ese número precedido de `v`.

#### Scenario: Primer release

- **WHEN** se publica el primer release del fork
- **THEN** su tag es `v1.0.0` y la app de Windows y el overlay de Linux reportan `1.0.0`

#### Scenario: Tag que no coincide con el código

- **WHEN** se publica el tag `v1.2.0` pero el código declara la versión `1.1.0`
- **THEN** la construcción del release falla y no se publica ningún archivo

### Requirement: Archivos de Windows

Cada release SHALL incluir un instalador `BeyondMeter-Setup-v<versión>.exe` y un
zip portátil `BeyondMeter-v<versión>-win-x64.zip`, ambos con todo lo necesario
para correr sin instalar .NET, más copias con nombre fijo
`BeyondMeter-Setup.exe` y `BeyondMeter-win-x64.zip`, para que
`releases/latest/download/<nombre>` siempre dé la última versión.

#### Scenario: Enlace fijo al instalador

- **WHEN** alguien descarga `releases/latest/download/BeyondMeter-Setup.exe`
- **THEN** obtiene el instalador del último release

#### Scenario: Zip portátil

- **WHEN** se descomprime el zip portátil en una PC con Windows 10 sin .NET
- **THEN** `BeyondMeter.exe` arranca y trae la carpeta de datos, la licencia y las instrucciones

### Requirement: Archivo de Linux

Cada release SHALL incluir `beyond-meter-v<versión>-linux-x64.tar.gz` y una copia
con nombre fijo `beyond-meter-linux-x64.tar.gz`, con el overlay listo para
correr sin instalar .NET, su carpeta de datos, la licencia y un script de
instalación. El script SHALL instalar el overlay en la carpeta del usuario,
SHALL dejar el comando `aionmeter-overlay` disponible, SHALL dar al binario el
permiso de captura sin root y SHALL poder ejecutarse de nuevo para actualizar.

#### Scenario: Instalar desde el tarball

- **WHEN** en una distro x64 con libpcap se descomprime el tarball y se ejecuta el script de instalación
- **THEN** `aionmeter-overlay` queda en el PATH del usuario y captura sin correr como root

#### Scenario: Actualizar desde un tarball nuevo

- **WHEN** ya hay una versión instalada y se ejecuta el script de un release más nuevo
- **THEN** queda instalada la versión nueva con el permiso de captura reaplicado, y los ajustes y el historial del usuario siguen intactos

### Requirement: Código fuente

Cada release SHALL incluir un zip del código fuente sin grabaciones de
tráfico, salidas de compilación ni herramientas locales.

#### Scenario: Sin grabaciones

- **WHEN** el repositorio de trabajo tiene archivos `.pcap` en `captures/`
- **THEN** el zip de código fuente no contiene ningún `.pcap`

### Requirement: Construcción automática

Publicar un tag `v*` en el repositorio del fork SHALL construir todos los
archivos de Windows y Linux, SHALL correr los tests y SHALL crear el release con
esos archivos solo si todo lo anterior terminó bien. Las notas del release
SHALL indicar la versión del original en la que se basa.

#### Scenario: Tests en rojo

- **WHEN** se publica un tag y algún test falla
- **THEN** no se crea el release

#### Scenario: Tag de prueba

- **WHEN** se publica el tag `v1.0.0-test` y el código declara la versión `1.0.0`
- **THEN** se crea un pre-release con los mismos archivos, que la app de Windows no ofrece como actualización

#### Scenario: Release completo

- **WHEN** se publica el tag `v1.0.0` y la construcción termina bien
- **THEN** el release `v1.0.0` tiene los archivos de Windows, el de Linux y el código fuente, y sus notas dicen en qué versión del original se basa
