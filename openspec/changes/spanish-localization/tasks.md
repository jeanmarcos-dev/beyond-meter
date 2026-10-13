# Tasks

## 1. Textos de la interfaz testeables

- [x] 1.1 Convertir `UiText` en `partial class` y enlazar `UiText.cs` y `AppInfo.cs` en `tests/AionMeter.Tests/AionMeter.Tests.csproj` con `<Compile Include … Link>`; agregar un test que construya `UiText` y lea `Code`; verificar que compila y pasa con `dotnet test tests/AionMeter.Tests/AionMeter.Tests.csproj --filter UiTextTests` y que la App compila con `-p:EnableWindowsTargeting=true`
- [x] 1.2 Tests de idiomas en `UiTextTests`: `Languages` es `en, es, ru`; `Normalize` devuelve `es` para `es` y `en` para un código desconocido o nulo; `Use("es")` deja `Current.Code == "es"`, `Culture` en español y `DateFormat` que formatea el 13/10/2026 como "13 de octubre de 2026"; verificar que fallan

## 2. Español en `UiText`

- [x] 2.1 Crear `src/AionMeter.App/Services/UiText.Es.cs` con la instancia `Es` y cada propiedad traducida en español neutro con tuteo (textos, `ClassNames`, `FlagNames`, `Chat`, `Players`), enlazarlo en los tests, y resolver `Use`, `Normalize`, `Culture` y `DateFormat` con la tabla de idiomas; verificar que pasan los tests de la 1.2
- [x] 2.2 Tests de cobertura en `UiTextTests`: cada propiedad `string` pública de `Es` difiere del inglés salvo la lista de excepciones, sus marcadores de formato coinciden con los del inglés, `ClassNames`, `FlagNames` y `Chat` tienen las mismas claves sin valores en inglés, y `Players(1)` y `Players(3)` están en español; verificar que pasan y que fallan si se borra una traducción de `UiText.Es.cs`
- [x] 2.3 Reemplazar el formato de fecha de `UpdateWindow.FormatDate` por `UiText.Current.DateFormat` y actualizar el comentario de `AppSettings.Language` con los tres idiomas; verificar que la App compila sin warnings con `-p:EnableWindowsTargeting=true`

## 3. Selector de idioma

- [x] 3.1 Agregar "Español" (`Tag="es"`) al combo de idioma de `SettingsWindow` y seleccionar el ítem por `Tag` en vez de por índice; verificar que la App compila y que el test de identidad sigue pasando
- [x] 3.2 Reemplazar el elemento `SwitchLanguage` de la bandeja y `App.ToggleLanguage` por un submenú con los tres idiomas (texto `LanguageMenu` en `UiText`, en los tres idiomas) que marque el actual y llame a `SetLanguage`, y hacer que el botón de idioma del overlay abra un menú con los mismos tres idiomas en lugar de alternar; quitar `SwitchLanguage` de los tres idiomas; verificar que la App compila sin warnings y que pasan los tests de cobertura

## 4. Notas de release por idioma

- [x] 4.1 Tests en `UpdateTests` para `ReleaseNotes.Lines`: notas con marcas `<!-- lang: en -->`, `<!-- lang: es -->` y `<!-- lang: ru -->` muestran solo la sección pedida; notas en/ru sin marcas pedidas en `es` se muestran completas; las notas en/ru existentes siguen dando el mismo resultado en `ru` y en `en`; las marcas no aparecen como texto; verificar que fallan las nuevas
- [x] 4.2 Implementar en `ReleaseNotes` el idioma por marca, con el cirílico como respaldo; verificar que pasan la 4.1 y todos los tests de `UpdateTests`
- [x] 4.3 Documentar en la sección de releases del `README.md` cómo marcar las secciones por idioma y actualizar la línea de idiomas de la interfaz a "English, Spanish and Russian"; verificar que el ejemplo del README se parsea con `ReleaseNotes.Lines` en un test

## 5. Instalador e instrucciones

- [x] 5.1 Agregar el idioma `es` con `compiler:Languages\Spanish.isl` a `installer/AION2DpsMeter.iss` y los mensajes `es.*` de cada mensaje `en.*` existente; agregar un test que compruebe que cada clave `en.X` del `.iss` tiene su `es.X`; verificar que el test pasa
- [x] 5.2 Agregar la sección "ESPAÑOL" a `INSTALL.txt` con los mismos pasos que la inglesa; verificar que el test de identidad sigue pasando y comparando a mano que la sección tiene los mismos apartados que la inglesa

## 6. Verificación integral

- [x] 6.1 Correr la suite completa, `dotnet format --verify-no-changes` sobre Tests (excluyendo `ChatLineTests.cs`) y Cli, el build de la App con `-p:EnableWindowsTargeting=true`, y publicar un tag de prueba con sufijo para que el CI compile el instalador con `Spanish.isl`, previa confirmación del usuario; verificar todo en verde y borrar el pre-release y el tag de prueba
- [ ] 6.2 Verificación manual en Windows (a cargo del usuario): `BeyondMeter.exe --render-sample <carpeta> --lang es` para cada ventana (`--window breakdown|timers|update`), y cambiar el idioma desde Ajustes y desde la bandeja con la app abierta; verificar que no hay textos cortados ni en inglés y que el instalador en español muestra la página de Npcap traducida
