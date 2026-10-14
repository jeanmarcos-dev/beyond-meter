## Purpose

Mostrar en el overlay la latencia real del juego, leída de forma pasiva del
intercambio de ping que el propio cliente hace con el servidor, para que el
jugador sepa si un problema en la pelea viene de su conexión.

## ADDED Requirements

### Requirement: Medición pasiva del ping del juego

El medidor SHALL obtener la latencia a partir de las respuestas de ping que el
servidor envía al cliente, que devuelven el timestamp del pedido. NO SHALL
enviar, inyectar ni modificar paquetes para medirla. Cada respuesta válida
SHALL producir una muestra de latencia en milisegundos enteros.

#### Scenario: Respuesta de ping del servidor

- **WHEN** llega una respuesta de ping cuyo pedido salió del cliente 64 ms
  antes
- **THEN** el ping vigente pasa a ser 64 ms

#### Scenario: Respuesta retransmitida

- **WHEN** la misma respuesta de ping se captura dos veces por una
  retransmisión de TCP
- **THEN** se cuenta una sola muestra

### Requirement: Formatos del timestamp del cliente

El medidor SHALL interpretar los dos formatos de timestamp que usa el cliente:
el contador de rendimiento del PC en milisegundos más el desplazamiento fijo de
16 777 216 000 ms (servidores Global) y los milisegundos UTC desde la época de
.NET (servidores KR/TW).

#### Scenario: Cliente Global

- **WHEN** el timestamp devuelto es el contador de rendimiento más el
  desplazamiento fijo, y la respuesta llega 45 ms después de ese instante del
  contador
- **THEN** el ping es 45 ms

#### Scenario: Cliente KR/TW

- **WHEN** el timestamp devuelto está en milisegundos UTC desde la época de
  .NET y la respuesta llega 80 ms después de esa hora
- **THEN** el ping es 80 ms, sin necesidad de haber visto el pedido

### Requirement: Respaldo por emparejamiento de pedido y respuesta

Cuando el contador de rendimiento no está disponible o no coincide con el
reloj del juego, el medidor SHALL medir el ping como el tiempo entre el pedido
de ping capturado en el sentido cliente→servidor y su respuesta. Una vez que
dos pings consecutivos coinciden en la diferencia entre el reloj del juego y el
de la captura (tolerancia de 15 ms), SHALL usar esa diferencia para las
respuestas cuyo pedido no se capturó. Si dos pings seguidos muestran que el
contador difiere en más de 25 ms de lo medido por emparejamiento, el medidor
SHALL dejar de usar el contador hasta que se reinicie la captura.

#### Scenario: Contador no disponible

- **WHEN** el contador no está disponible, se captura un pedido de ping y su
  respuesta llega 64 ms después
- **THEN** el ping es 64 ms

#### Scenario: Pedido no capturado tras aprender la diferencia

- **WHEN** dos pings emparejados ya fijaron la diferencia entre relojes y llega
  una respuesta sin pedido capturado
- **THEN** el ping se calcula con esa diferencia

#### Scenario: Salto del reloj del sistema

- **WHEN** el reloj del sistema salta 2 s durante la sesión
- **THEN** el siguiente ping emparejado se informa sin ese salto, y una
  respuesta sin pedido no se informa hasta que dos pings vuelvan a coincidir
  en la diferencia

#### Scenario: El contador no es el del juego

- **WHEN** en dos pings seguidos el contador y el emparejamiento difieren en
  más de 25 ms
- **THEN** a partir de ahí el ping sale solo del emparejamiento

### Requirement: Muestras fuera de rango

El medidor SHALL descartar como inválida cualquier muestra menor de 1 ms o
mayor de 9999 ms. Una muestra descartada NO SHALL cambiar el ping vigente.

#### Scenario: Timestamp incoherente

- **WHEN** el cálculo de una respuesta da −300 ms
- **THEN** el ping vigente no cambia

### Requirement: Ciclo de vida del ping vigente

El ping vigente SHALL ser la última muestra válida. SHALL borrarse cuando la
captura cambie de conexión del juego o se detenga, y cuando pasen 30 s sin una
muestra válida.

#### Scenario: Reconexión

- **WHEN** el juego cambia de servidor y la captura pasa a otra conexión
- **THEN** el ping vigente queda sin dato hasta la primera respuesta de la
  conexión nueva

#### Scenario: Sin respuestas

- **WHEN** pasan 30 s sin ninguna respuesta de ping válida
- **THEN** el ping vigente queda sin dato

### Requirement: Repeticiones de capturas grabadas

Al repetir una captura grabada, el medidor NO SHALL usar el contador de
rendimiento del PC actual. El ping SHALL salir del emparejamiento pedido y
respuesta o, en el formato KR/TW, de la hora del timestamp.

#### Scenario: Repetir una captura Global

- **WHEN** se repite una captura de un cliente Global que contiene pedidos y
  respuestas de ping
- **THEN** el ping sale del tiempo entre cada pedido y su respuesta en la
  captura

### Requirement: Indicador de ping en el overlay

El overlay SHALL mostrar el ping vigente en su línea superior como un ícono de
cuatro barras de señal de altura creciente seguido de `<n> ms`, o `— ms` sin
dato. El ícono y el texto SHALL tomar el color del nivel: verde de 0 a 99 ms,
amarillo de 100 a 170 ms y rojo desde 171 ms; sin dato, gris. El amarillo SHALL
distinguirse del dorado del título y del reloj de la pelea. El indicador SHALL
verse también sin pelea en curso.

#### Scenario: Ping bueno

- **WHEN** el ping vigente es 99 ms
- **THEN** el overlay muestra las barras y `99 ms` en verde

#### Scenario: Ping regular

- **WHEN** el ping vigente es 100 ms o 170 ms
- **THEN** el overlay muestra las barras y el valor en amarillo

#### Scenario: Ping malo

- **WHEN** el ping vigente es 171 ms
- **THEN** el overlay muestra las barras y `171 ms` en rojo

#### Scenario: Sin dato

- **WHEN** todavía no llegó ninguna respuesta de ping
- **THEN** el overlay muestra las barras y `— ms` en gris

### Requirement: Preferencia para ocultar el ping

*Ajustes → Overlay* SHALL ofrecer una opción para mostrar u ocultar el ping,
activada por defecto, que se guarda con el resto de las preferencias. Con la
opción desactivada, el overlay NO SHALL mostrar el indicador; la medición
puede seguir.

#### Scenario: Ocultar el ping

- **WHEN** el jugador desactiva la opción del ping
- **THEN** el indicador desaparece de la línea superior y sigue oculto tras
  reiniciar el medidor

### Requirement: Textos traducidos

Las cadenas nuevas del indicador y de la preferencia SHALL existir en inglés,
español y ruso.

#### Scenario: Interfaz en ruso

- **WHEN** el idioma de la interfaz es ruso
- **THEN** la opción del ping en Ajustes aparece en ruso
