# Tasks

## 1. Política de visibilidad

- [x] 1.1 Escribir tests de `VisibilityPolicy` para cada escenario del spec: juego cerrado oculto, juego abierto visible, auto-ocultar a los 60 s, 0 desactiva, sin fijar no se oculta, auto-mostrar al pelear, ocultar a mano manda hasta reabrir el juego; verificar que fallan con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter VisibilityPolicy`
- [x] 1.2 Implementar `VisibilityPolicy`; verificar que pasan los tests de la 1.1

## 2. Integración en el overlay

- [x] 2.1 Tests headless: la ventana aplica la política en el temporizador, un auto-mostrar no activa la ventana y una pelea con el overlay oculto se guarda en el historial; verificar que fallan
- [x] 2.2 Conectar la política en `OverlayWindow`, añadir la preferencia de segundos de auto-ocultar con su rango y el flag `--hidden`; verificar que pasan la 2.1 y el test de parseo de preferencias

## 3. Arranque con la sesión

- [x] 3.1 Escribir `packaging/linux/aionmeter-overlay.service` y `packaging/linux/install-service.sh`; verificar con `systemd-analyze --user verify packaging/linux/aionmeter-overlay.service` y `shellcheck packaging/linux/install-service.sh`
- [x] 3.2 Comprobar que `quit` sale con código 0 y que una excepción no controlada sale con código distinto de 0; verificar con un test que lance el binario en modo demo y lea el código de salida
- [x] 3.3 Documentar la instalación de la unidad en el README; verificar que el script documentado la deja habilitada con `systemctl --user is-enabled aionmeter-overlay`

## 4. Cierre

- [x] 4.1 Correr la suite completa y `dotnet format --verify-no-changes`; verificar salida limpia
- [ ] 4.2 Instalar la unidad, cerrar y reabrir sesión, abrir y cerrar el juego; verificar que el overlay aparece y se oculta solo
