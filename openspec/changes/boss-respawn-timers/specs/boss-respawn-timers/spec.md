## Purpose

Saber desde el overlay cuándo reaparece cada jefe de campo del servidor propio y
recibir un aviso a tiempo para los que el jugador vigila.

## ADDED Requirements

### Requirement: Fuentes de los timers

Los timers SHALL alimentarse de la lista de jefes del mapa que envía el juego,
de los jefes vistos vivos o muertos y de las muertes que el medidor presencia.
Cuando la lista del juego es la información más reciente, su hora de
reaparición SHALL prevalecer sobre la calculada por el medidor.

#### Scenario: El juego informa la hora de reaparición

- **WHEN** llega la lista del mapa con un jefe muerto que vuelve a las 21:40
- **THEN** el panel muestra ese jefe con reaparición a las 21:40

#### Scenario: Muerte presenciada con intervalo conocido

- **WHEN** el medidor ve morir a un jefe cuyo intervalo es de 60 minutos
- **AND** no llega una lista del juego posterior
- **THEN** el panel muestra la reaparición 60 minutos después de esa muerte

### Requirement: Timers por servidor

Los timers SHALL guardarse por servidor y el panel SHALL mostrar los del
servidor del personaje conectado, cambiando solo cuando el personaje cambia de
servidor.

#### Scenario: Cambiar de personaje a otro servidor

- **WHEN** el jugador entra con un personaje de otro servidor
- **THEN** el panel pasa a los timers de ese servidor
- **AND** los del servidor anterior se conservan

### Requirement: Estado visible de cada jefe

El panel SHALL mostrar por jefe su nombre, zona y uno de estos estados: vivo
desde una hora, cuenta atrás hasta la reaparición, "debería estar vivo" cuando
la hora ya pasó sin confirmación, o "intervalo desconocido".

#### Scenario: Reaparición vencida

- **WHEN** la hora prevista de un jefe pasó hace 3 minutos y nadie lo vio vivo
- **THEN** el jefe aparece como "debería estar vivo"

### Requirement: Acciones sobre un jefe

Con el overlay sin fijar, el jugador SHALL poder vigilar o dejar de vigilar un
jefe, fijar su intervalo en minutos, marcarlo muerto ahora y quitarlo de la
lista. Los cambios SHALL persistir entre reinicios.

#### Scenario: Marcar muerto a mano

- **WHEN** el jugador marca muerto un jefe con intervalo de 120 minutos
- **THEN** la cuenta atrás empieza en 120 minutos

### Requirement: Aviso previo

Para cada jefe vigilado del servidor actual, el overlay SHALL emitir un único
aviso de escritorio con sonido cuando falten los minutos de antelación
configurados (por defecto 2) para su reaparición. Un jefe que ya se ve vivo NO
SHALL avisar. Si las herramientas de aviso no están disponibles, el overlay
SHALL seguir funcionando sin avisar.

#### Scenario: Aviso a tiempo

- **WHEN** un jefe vigilado reaparece a las 21:40 y la antelación es 2 minutos
- **THEN** a las 21:38 aparece un aviso con su nombre y zona
- **AND** no se repite para esa misma reaparición

#### Scenario: Jefe no vigilado

- **WHEN** un jefe no vigilado llega a su hora de reaparición
- **THEN** no se emite aviso

### Requirement: Panel y resumen

El comando `timers` SHALL abrir y cerrar el panel. Con el panel cerrado, el
overlay SHALL mostrar en una línea el próximo jefe vigilado y su cuenta atrás,
y ocultarla si no hay ninguno.

#### Scenario: Resumen con el overlay fijado

- **WHEN** el overlay está fijado y hay un jefe vigilado que vuelve en 12:34
- **THEN** se ve una línea con su nombre y `12:34`

### Requirement: Demo y repeticiones no alteran los timers

Los eventos del modo demo o de una repetición de captura NO SHALL modificar los
timers guardados.

#### Scenario: Demo con jefe

- **WHEN** corre el demo, que mata a un jefe
- **THEN** el archivo de timers queda igual que antes del demo
