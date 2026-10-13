# Tasks

## 1. Modelo del desglose

- [x] 1.1 Escribir tests de `BreakdownView`: tabla completa con "-" en tasas sin golpes, serie suavizada a 3 s con caída en una pausa, eje de la duración de la pelea, rotación sin ticks con tamaño por daño y marca de crítico, defensa con daño, daño por segundo y golpes; verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter BreakdownView`
- [x] 1.2 Implementar `BreakdownView` sobre `CombatantDetail`; verificar que pasan los tests de la 1.1

## 2. Ventana y navegación

- [x] 2.1 Tests headless: "Ver detalle" en el panel abre la ventana con ese jugador, el comando `breakdown` la abre con el propio, elegir otro jugador cambia las pestañas, una pelea del historial no cambia entre refrescos; verificar que fallan
- [x] 2.2 Implementar la ventana con la lista del grupo y las pestañas Skills y Defensa, el botón en `SkillPanel` y `OverlayCommand.Breakdown`; verificar que pasan los tests de la 2.1

## 3. Gráficos

- [x] 3.1 Implementar el control de línea de tiempo que dibuja las series de `BreakdownView` y un test headless que compruebe que se renderiza sin excepciones con una pelea de 10 minutos; verificar con `--filter OverlayWindowTests`
- [x] 3.2 Implementar la rotación virtualizada con tooltip de skill y daño, y un test headless con 20 000 lanzamientos que mida que el refresco no pasa de 100 ms; verificar el test

## 4. Cierre

- [x] 4.1 Documentar el desglose y el comando en el README; verificar que el comando documentado abre la ventana
- [x] 4.2 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 4.3 Publicar, reaplicar `setcap` y abrir el desglose durante una pelea real; verificar que la curva y la rotación avanzan en vivo
