# Tasks

## 1. Archivo de preferencias tolerante

- [x] 1.1 Escribir tests de `OverlaySettings.Parse`: defaults con archivo vacío, rangos acotados (opacidad 5 → 1, timeouts fuera de límite), modo de historial desconocido sin perder la posición, JSON corrupto, archivo viejo sin claves nuevas; verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter OverlayTests`
- [x] 1.2 Implementar `Parse` por clave con `JsonDocument` y las propiedades nuevas (modo de objetivo, idioma, timeouts, política de historial, opacidad); verificar que pasan los tests de la 1.1 y los de round-trip existentes

## 2. Aplicar las preferencias al medidor

- [x] 2.1 Tests de `MeterSession` con `HistoryPolicy`: mob de 12 s no se guarda, jefe de 8 s sí, nada de menos de 5 s en ningún modo, modo "todas" guarda el mob; verificar que fallan
- [x] 2.2 Reemplazar `minSavedFightMs` por `HistoryPolicy` en `MeterSession` y cablearla desde `OverlayApp`; verificar que pasan la 2.1 y la suite
- [x] 2.3 Mapear modo de objetivo y timeouts a `MeterOptions` y el idioma a `GameData` al arrancar; test con `GameData` de prueba en `es` que confirme el nombre traducido y el fallback a inglés; verificar con el filtro de `OverlayTests`

## 3. Control en la barra y comandos

- [x] 3.1 Test headless: el botón "Jefes/Todo" alterna el texto, cierra la pelea en curso como `Manual`, cambia `MeterOptions.TargetMode` y escribe el archivo; verificar que falla
- [x] 3.2 Añadir el botón y `OverlayCommand.Mode`; verificar que pasa la 3.1 y el test de parseo del comando
- [x] 3.3 Test headless de `reload`: editar idioma y opacidad en el archivo, ejecutar el comando y comprobar nombres, opacidad y que la pelea conserva su daño; verificar que falla y luego implementar `OverlayCommand.Reload` hasta que pase
- [x] 3.4 Aplicar la opacidad configurada al fondo fijado con su test headless; verificar con `--filter OverlayWindowTests`

## 4. Documentación y cierre

- [x] 4.1 Documentar en el README cada clave de `overlay.json` con rango y default, y el cambio del valor por defecto del historial; verificar que los nombres de clave coinciden con los de `Parse`
- [x] 4.2 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 4.3 Publicar, reaplicar `setcap` y comprobar en el juego que el modo "Todo" muestra daño contra un mob normal
