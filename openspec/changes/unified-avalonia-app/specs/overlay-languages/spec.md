## Purpose

Que toda la interfaz del overlay se pueda usar en inglés, español y ruso, con los
mismos textos en Windows y Linux y sin textos que queden en otro idioma.

## ADDED Requirements

### Requirement: Idiomas del overlay

Cada texto visible del overlay y de sus ventanas SHALL salir de la tabla de
textos de la interfaz, en inglés, español o ruso según el idioma elegido. No
SHALL quedar textos fijos en el código. El idioma SHALL cambiarse desde Ajustes,
desde la bandeja y desde el botón de idioma del overlay, y SHALL aplicarse sin
reiniciar.

#### Scenario: Cambiar a ruso

- **WHEN** el jugador elige "Русский" en el botón de idioma del overlay
- **THEN** el overlay, el desglose, el panel de timers y la bandeja pasan a ruso

### Requirement: Cobertura en los tres idiomas

Cada texto SHALL tener versión en español y en ruso, con los mismos valores
variables que el texto en inglés.

#### Scenario: Texto nuevo sin traducir

- **WHEN** se agrega un texto en inglés sin su versión en español o en ruso
- **THEN** los tests fallan nombrando ese texto

### Requirement: Idioma inicial

Una instalación nueva SHALL arrancar en inglés. Una instalación existente SHALL
conservar el idioma que el jugador veía: en Windows, el de la app WPF; en Linux,
español.

#### Scenario: Usuario de Linux que actualiza

- **WHEN** un jugador de Linux con `overlay.json` instala la versión nueva
- **THEN** la interfaz sigue en español
