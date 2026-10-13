## Purpose

Garantizar que Beyond Meter sea una sola app con la misma interfaz en Windows y
Linux, que guarde los datos donde cada sistema espera y que reemplace a la app
WPF solo cuando no se pierda ninguna función.

## ADDED Requirements

### Requirement: Misma interfaz en Windows y Linux

Windows y Linux SHALL usar la misma app, con las mismas ventanas, paneles,
botones y textos. Solo SHALL cambiar lo que depende del sistema: ubicación de los
datos, instalación y actualización, y la integración con el escritorio.

#### Scenario: Mismo overlay

- **WHEN** se abre Beyond Meter en Windows y en Linux con la misma pelea guardada
- **THEN** el overlay muestra las mismas columnas, botones y textos en los dos

### Requirement: Datos en el lugar de cada sistema

En Windows la app SHALL guardar ajustes, peleas, caché de nombres, timers de
jefes, grabaciones y logs en `%AppData%\AionMeter`. En Linux SHALL usar las
rutas XDG que ya usa el overlay. En Windows SHALL leer sin conversión las
peleas, el caché de nombres y los timers que dejó la app WPF.

#### Scenario: Actualizar desde la app WPF

- **WHEN** un jugador con peleas y timers guardados por la app WPF instala la versión con el overlay Avalonia
- **THEN** el historial muestra esas peleas y el panel de timers muestra esos timers

### Requirement: Paridad antes del reemplazo

El release de Windows SHALL seguir publicando la app WPF hasta que el overlay
tenga todas estas funciones en Windows:
- ventana de Ajustes;
- bandeja con su menú;
- atajos globales;
- click-through;
- mostrarse y ocultarse solo;
- instancia única;
- auto-update con ventana de novedades;
- aviso de Npcap;
- íconos de skills y retratos de jefes;
- columnas GS/CP;
- tamaño de fila;
- barra de HP y límites de tiempo del jefe;
- historial;
- desglose con precisión, rotación y defensa;
- timers de jefes con alertas;
- copiar al chat la línea del grupo y la de un jugador, y la tabla para Discord;
- idiomas inglés, español y ruso;
- demo, replay y grabación;
- logs.

#### Scenario: Falta una función

- **WHEN** el overlay todavía no tiene atajos globales en Windows
- **THEN** el release de Windows sigue publicando la app WPF

### Requirement: Reemplazo de la app WPF

Una vez cumplida la paridad, el release de Windows SHALL publicar el overlay como
`BeyondMeter.exe`, con el mismo instalador, AppId, nombre y feed de
actualizaciones. Una copia WPF instalada SHALL actualizarse a la versión con el
overlay por el auto-update de siempre. Después del reemplazo, el repositorio
SHALL dejar de contener la app WPF.

#### Scenario: Auto-update hacia el overlay

- **WHEN** una copia instalada de la app WPF encuentra el release con el overlay
- **THEN** se actualiza sola y, al reiniciarse, abre el overlay Avalonia con los ajustes, el historial y los timers de antes
