## Purpose

Permitir que el jugador configure la app desde una ventana de Ajustes igual en
Windows y Linux, con un único formato de configuración que conserve lo que cada
plataforma guardaba antes.

## ADDED Requirements

### Requirement: Ventana de Ajustes

La app SHALL tener una ventana de Ajustes, accesible desde la bandeja y desde el
overlay, con las secciones idioma, overlay, medidor, historial, captura, atajos,
actualizaciones y datos. Cada opción que hoy ofrece la app WPF o el
`overlay.json` de Linux SHALL estar en la ventana. Guardar SHALL aplicar los
cambios sin reiniciar. Las opciones que solo existen en un sistema (por ejemplo,
el adaptador de Npcap) SHALL verse solo en ese sistema.

#### Scenario: Cambiar el modo de medición

- **WHEN** el jugador elige "Todo lo que golpeas" en Ajustes y guarda
- **THEN** el overlay empieza a contar el daño contra mobs sin reiniciar

#### Scenario: Cancelar

- **WHEN** el jugador cambia opciones en Ajustes y pulsa Cancelar
- **THEN** no cambia ningún ajuste ni el archivo de configuración

### Requirement: Un solo formato de configuración

Windows y Linux SHALL guardar la configuración en el mismo formato. Un valor
fuera de rango SHALL ajustarse al límite más cercano, y un valor ilegible SHALL
volver a su valor por defecto sin afectar a los demás.

#### Scenario: Valor ilegible

- **WHEN** el archivo de configuración trae un texto donde va un número
- **THEN** esa opción toma su valor por defecto y las demás conservan el suyo

### Requirement: Migración de la configuración anterior

En el primer arranque sin configuración nueva, la app SHALL importar los
valores equivalentes de la configuración anterior: `settings.json` de la app WPF
en Windows, y `overlay.json` en Linux. Los archivos anteriores SHALL quedar sin
modificar.

#### Scenario: Migrar desde WPF

- **WHEN** en Windows existe `settings.json` de la app WPF con idioma ruso y atajos personalizados, y no hay configuración nueva
- **THEN** la app arranca en ruso con esos atajos

#### Scenario: Migrar desde overlay.json

- **WHEN** en Linux existe `overlay.json` con posición, ancho y modo "Todo", y no hay configuración nueva
- **THEN** el overlay arranca en esa posición, con ese ancho, en modo "Todo" y con la interfaz en español
