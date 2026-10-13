## Purpose

Definir en qué idiomas se muestra la interfaz de la app de Windows, cómo elige el
jugador el idioma y qué textos debe cubrir cada uno, para que ningún idioma quede
a medias.

## ADDED Requirements

### Requirement: Idiomas disponibles

La app de Windows SHALL ofrecer la interfaz en inglés, español y ruso. El español
SHALL ser neutro y tratar al jugador de "tú". Un idioma guardado que la app no
conoce SHALL tratarse como inglés.

#### Scenario: Elegir español

- **WHEN** el jugador elige "Español" en Ajustes y guarda
- **THEN** ventanas, bandeja y mensajes pasan a español sin reiniciar la app, y los nombres de skills y NPC se recargan en español

#### Scenario: Idioma desconocido en los ajustes

- **WHEN** el archivo de ajustes guarda un idioma que la app no ofrece
- **THEN** la app arranca en inglés

#### Scenario: Primer arranque

- **WHEN** la app arranca sin ajustes guardados en un Windows en español
- **THEN** la interfaz está en inglés

### Requirement: Selector de idioma

Ajustes SHALL listar los tres idiomas con su nombre propio (English, Español,
Русский) y marcar el idioma en uso. El menú de la bandeja SHALL ofrecer un
submenú con los tres idiomas que marque el idioma en uso y lo cambie con un solo
clic. El botón de idioma del overlay SHALL abrir un menú con los mismos tres
idiomas, con el idioma en uso marcado.

#### Scenario: Cambiar desde la bandeja

- **WHEN** la interfaz está en ruso y el jugador elige "Español" en el submenú de idioma de la bandeja
- **THEN** la interfaz pasa a español y el submenú marca "Español"

#### Scenario: Cambiar desde el overlay

- **WHEN** el jugador pulsa el botón de idioma del overlay y elige "Русский"
- **THEN** la interfaz pasa a ruso y el botón muestra el código del ruso

### Requirement: Cobertura completa del español

Cada texto que la interfaz muestra en inglés SHALL tener su versión en español,
incluidos los nombres de clases, los efectos de golpe y las palabras de la línea
de chat. Cada versión SHALL conservar los mismos valores variables (cantidades,
nombres, fechas) que el texto en inglés.

#### Scenario: Línea de chat en español

- **WHEN** la interfaz está en español y el jugador copia la línea de un jefe muerto
- **THEN** la línea usa las palabras en español para el resultado, el puesto y el grupo, con las mismas cifras que en inglés

#### Scenario: Texto con valores

- **WHEN** la interfaz está en español y hay una versión nueva disponible
- **THEN** el aviso muestra el número de la versión nueva dentro del texto en español

### Requirement: Fechas en el idioma de la interfaz

Las fechas que muestra la interfaz en español SHALL usar nombres de mes en
español y el orden día, mes, año.

#### Scenario: Fecha de un release

- **WHEN** la interfaz está en español y la ventana de actualización muestra un release publicado el 13 de octubre de 2026
- **THEN** la fecha se lee "13 de octubre de 2026"

### Requirement: Notas de release por idioma

La ventana de actualización SHALL mostrar, de las notas de un release, solo las
secciones escritas en el idioma de la interfaz cuando las notas traen secciones
en más de un idioma. Si no hay secciones en ese idioma, SHALL mostrar las notas
completas. Una sección SHALL poder marcarse como inglés, español o ruso sin que
la marca se vea en la página del release en GitHub. Una sección sin marca SHALL
contar como ruso si su título tiene letras cirílicas, y como inglés en otro
caso.

#### Scenario: Notas en tres idiomas

- **WHEN** las notas traen secciones en inglés, español y ruso y la interfaz está en español
- **THEN** la ventana muestra solo las secciones en español

#### Scenario: Notas sin español

- **WHEN** las notas traen secciones en inglés y ruso, sin marcas, y la interfaz está en español
- **THEN** la ventana muestra las notas completas

#### Scenario: Notas del original

- **WHEN** las notas traen una sección rusa y una inglesa sin marcas, y la interfaz está en ruso
- **THEN** la ventana muestra solo la sección rusa, como antes
