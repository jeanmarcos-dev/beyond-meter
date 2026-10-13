## Purpose

Conservar el tráfico de una sesión de juego y volver a reproducirlo en el
overlay para revisar peleas o diagnosticar errores del medidor sin repetir la
pelea.

## ADDED Requirements

### Requirement: Grabar desde el overlay

El comando `record` SHALL empezar a grabar el tráfico del juego si no se está
grabando y detener la grabación si se está grabando, sin interrumpir la
medición. Cada grabación SHALL ir a un archivo nuevo en
`~/.local/share/aionmeter/captures/` con la fecha y hora en el nombre.

#### Scenario: Empezar a grabar en mitad de una pelea

- **WHEN** hay una pelea en curso y se ejecuta `record`
- **THEN** se crea un archivo `.pcap` nuevo que empieza a crecer
- **AND** la pelea en curso conserva su daño acumulado

#### Scenario: Detener la grabación

- **WHEN** se está grabando y se ejecuta `record`
- **THEN** el archivo deja de crecer y queda legible por la repetición

### Requirement: Indicador de grabación

Mientras se graba, la cabecera del overlay SHALL mostrar "REC" de forma
visible, también con el overlay fijado.

#### Scenario: Grabar con el overlay fijado

- **WHEN** el overlay está fijado y se ejecuta `record`
- **THEN** la cabecera muestra "REC"

### Requirement: Solo tráfico del juego

La grabación SHALL contener únicamente los paquetes de las conexiones del juego
que la captura ya filtra.

#### Scenario: Navegar mientras se graba

- **WHEN** se graba y a la vez se usa el navegador
- **THEN** la grabación no contiene tráfico de puertos 80 ni 443

### Requirement: Repetir una grabación

Al abrir el overlay con `--replay <archivo>`, SHALL mostrar las peleas de la
grabación como si fueran en vivo, a la velocidad original, o lo más rápido
posible con `--fast`. La cabecera SHALL indicar que es una repetición. Un
archivo inexistente o ilegible SHALL producir un mensaje de error y un código
de salida distinto de 0.

#### Scenario: Repetir una grabación

- **WHEN** se abre el overlay con `--replay` sobre una grabación con una pelea
  de jefe
- **THEN** la tabla muestra esa pelea con las cifras que tuvo en vivo
- **AND** la cabecera indica que es una repetición

#### Scenario: Archivo que no existe

- **WHEN** se ejecuta `aionmeter-overlay --replay /no/existe.pcap`
- **THEN** se informa el error y el proceso termina con código distinto de 0

### Requirement: Una repetición no deja rastro

Una repetición NO SHALL guardar peleas en el historial, NO SHALL modificar la
caché de nombres ni los timers de jefes, y NO SHALL coexistir con una captura
en vivo en el mismo proceso.

#### Scenario: Repetir una pelea larga

- **WHEN** termina la repetición de una pelea de jefe de 3 minutos
- **THEN** el historial tiene las mismas peleas que antes de repetir
