# Tasks

## 1. Preferencias de las partes

- [x] 1.1 Escribir tests de `UserSettings`: `HiddenParts` vacío por defecto; ida y vuelta por JSON; un nombre desconocido se ignora; `Shows`/`SetShown` de `Ping` y `BossBar` leen y escriben `ShowPing` y `ShowBossPanel`. Verificar que fallan
- [x] 1.2 Agregar `OverlayPart`, `HiddenParts`, `Shows`, `SetShown` y su lectura en `Parse`. Verificar que pasan

## 2. Lógica de bordes

- [x] 2.1 Escribir tests de `EdgeResize`: borde bajo el puntero (izquierdo, derecho, inferior, esquinas, ninguno; el superior no cuenta), ancho por la derecha y por la izquierda (con desplazamiento de la ventana), límites 280–2000, filas por alto de fila con redondeo y límites 1–20. Verificar que fallan
- [x] 2.2 Implementar `EdgeResize`. Verificar que pasan

## 3. Overlay

- [x] 3.1 Escribir tests headless: el `⚙` está en el encabezado y no en la barra, se oculta al fijar; el menú tiene las casillas y `Más ajustes…` abre Ajustes; desmarcar reloj, total, estado, próximo jefe, pie, una columna y un botón los oculta y se guarda; ocultar todos los botones oculta la barra; el ping del menú cambia `ShowPing`; guardar Ajustes conserva `HiddenParts`; el `⇔` no existe. Verificar que fallan
- [x] 3.2 Implementar el `⚙`, el menú y la visibilidad de cada parte en `OverlayWindow` y las columnas ocultables en `RowControl`. Verificar que pasan
- [x] 3.3 Escribir tests headless de los bordes: arrastrar el derecho y el izquierdo cambia ancho (y posición en el izquierdo) y se guarda; arrastrar el inferior cambia `MaxRows` y se guarda; fijado no cambia nada; el cursor cambia sobre un borde. Verificar que fallan
- [x] 3.4 Implementar los bordes y quitar el `⇔`. Verificar que pasan

## 4. Textos y documentación

- [x] 4.1 Agregar las cadenas del menú en `UiText`, `UiText.Es` y el bloque ruso
- [x] 4.2 Documentar el menú, los bordes y `HiddenParts` en `docs/linux-overlay.md`, `README.md` y `README.ru.md`

## 5. Verificación

- [x] 5.1 `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj` y `dotnet format --verify-no-changes` (salvo los dos archivos con errores previos)
- [ ] 5.2 Instalar el build en Linux y comprobar a mano el menú y los cuatro bordes con el overlay real (instalado; el menú se abre y muestra las casillas. Falta probar a mano el clic en las casillas y los bordes: bajo Wayland no se puede mover el puntero desde la terminal. El clic real dentro del menú queda cubierto por un test headless con el puntero)
