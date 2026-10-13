# Design

## Context

- **`UiText`** (`src/AionMeter.App/Services/UiText.cs`) es una clase con unas
  270 propiedades `init` que traen el inglés como valor por defecto. Además:
  - tiene diccionarios `ClassNames` y `FlagNames`, `Chat` (`ChatWords` de Core)
    y un `Func<int, string> Players`;
  - el ruso es una instancia estática `Ru` que sobrescribe esos valores;
  - una propiedad que `Ru` no sobrescribe cae al inglés sin aviso.
- **El idioma es binario en todos lados:**
  - `UiText.Languages = ["en", "ru"]`;
  - `Normalize` devuelve `ru` o `en`, y `Use` elige entre `Ru` y `new UiText()`;
  - `Culture` es `ru-RU` o `en-US`;
  - `App.ToggleLanguage` alterna, la bandeja muestra `SwitchLanguage`
    ("English → Русский") y el combo de Ajustes elige por índice 0/1;
  - `UpdateWindow.FormatDate` tiene un formato para ruso y otro para el resto.
- **El cambio en caliente ya existe:** `MeterService.SetLanguage` normaliza,
  recarga los nombres con `GameData.ReloadNames` (los datos `es` existen) y
  aplica `UiText.Use`. `App.SetLanguage` cierra y reabre ventanas.
- **`ReleaseNotes.Lines`** (Core) separa secciones por título y distingue solo
  "cirílico" contra "lo demás".
- **`UiText` no depende de WPF:** solo usa tipos de Core, LINQ y `AppInfo`. El
  proyecto de tests (`net10.0`) no referencia la App (`net10.0-windows`).
- **Instalador:** tiene `[Languages]` en y ru, más unos 20 mensajes propios por
  idioma. No fija `ShowLanguageDialog`, así que Inno muestra el diálogo de
  idioma con el de Windows preseleccionado y lo omite en modo silencioso.
- **`INSTALL.txt`:** tiene una sección en ruso y otra en inglés.

## Goals / Non-Goals

**Goals:**
- Que agregar el español no aumente los conflictos de cada merge con upstream,
  que modifica `UiText.cs` con frecuencia.
- Que un texto sin traducir o con valores variables desalineados se detecte en
  los tests, no en el juego.

**Non-Goals:**
- Detectar el idioma de Windows en la app.
- Traducir nombres de mapas de jefes (`field_boss_maps.json` solo tiene en/ru) o
  de mazmorras: en español caen al inglés, igual que hoy en otros idiomas.
- Traducir README, documentación o `README.ru.md`.
- Cambiar el overlay de Linux.

## Decisions

- **El español va en `UiText.Es.cs` como `partial`.** `UiText` pasa a ser
  `partial class` y el archivo nuevo contiene solo `private static readonly
  UiText Es = new() { … }`. `UiText.cs` solo cambia en las líneas de idiomas:
  `Languages`, `Normalize`, `Use`, `Culture` y una propiedad `DateFormat`. Se
  descarta escribirlo junto a `Ru` en `UiText.cs`, porque sumaría unas 280
  líneas a un archivo que upstream reescribe en cada release.

- **El idioma se resuelve con una tabla, no con ternarios.** `Use` busca el
  código en un diccionario `en → new UiText()`, `es → Es`, `ru → Ru`. `Normalize`
  devuelve el código si está en `Languages` y `en` si no. `Culture` pasa a
  `es-ES` para el español. El formato de fecha de `UpdateWindow` se mueve a una
  propiedad `DateFormat` de `UiText`: `"d 'de' MMMM 'de' yyyy"` en español, y los
  formatos actuales en inglés y ruso. Así no queda otro `if` por idioma fuera de
  `UiText`.

- **Los tests compilan `UiText`.** El proyecto de tests incluye
  `UiText.cs`, `UiText.Es.cs` y `AppInfo.cs` de la App con `<Compile Include …
  Link>`. Los tests usan reflexión sobre las instancias reales:
  - toda propiedad `string` pública tiene en español un valor distinto del
    inglés, salvo una lista explícita de textos que son iguales en ambos idiomas
    (por ejemplo "DPS");
  - los marcadores `{0}`, `{1:…}` de cada texto en español coinciden con los del
    inglés;
  - `ClassNames`, `FlagNames` y `Chat` tienen las mismas claves y ningún valor en
    inglés, y `Players` produce texto en español.

  Se descarta inspeccionar el texto del código fuente, que es frágil con
  cadenas concatenadas e interpoladas. También se descarta mover `UiText` a una
  biblioteca nueva, porque es un refactor grande que choca con upstream. Antes
  de cablear todo, una tarea comprueba que el archivo compila fuera de la App;
  si no compila, se replantea esta decisión.

- **Las notas marcan la sección con un comentario HTML.** Una línea
  `<!-- lang: es -->` (o `en`, `ru`) fija el idioma de las secciones que siguen,
  hasta la próxima marca. GitHub no muestra los comentarios. Sin marcas, el
  idioma se deduce como hoy (cirílico es ruso, el resto inglés), así que las
  notas del original y las de la 1.0.0 se ven igual. Si no hay secciones del
  idioma pedido, o todas son de ese idioma, se muestran las notas completas,
  como ahora. Se descarta detectar el español por palabras o por acentos: no es
  confiable para separarlo del inglés.

- **La bandeja usa un submenú de idiomas.** `SwitchLanguage` y
  `App.ToggleLanguage` se reemplazan por un `ToolStripMenuItem` "Idioma" (texto
  `LanguageMenu` en `UiText`) con un elemento por idioma, cada uno con su nombre
  propio y una marca en el actual. Lo maneja la acción
  `SetLanguage(string)`, que ya existe. Se descarta ciclar en/es/ru con un solo
  elemento, porque obliga a pasar por un idioma que no se quiere.

- **Ajustes elige por código, no por índice.** El combo agrega
  `<ComboBoxItem Content="Español" Tag="es" />` y selecciona el ítem cuyo `Tag`
  coincide con el idioma, en lugar de calcular índices 0/1.

- **Instalador:** se agrega
  `Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"`, que Inno Setup 7
  trae entre sus traducciones oficiales, y los mensajes `es.*` propios. El
  diálogo de idioma sigue con el comportamiento por defecto de Inno.

## Risks / Trade-offs

- [Upstream agrega textos a `UiText` en un merge y quedan sin traducir] → El
  test de cobertura falla en ese mismo merge y lista las propiedades que faltan.
- [Un texto en español que, por ser igual al inglés, el test confunde con uno
  sin traducir] → Lista explícita de excepciones en el test, revisada a mano.
- [`UiText.cs` no compila fuera de la App por una dependencia oculta] → Lo
  verifica la primera tarea, antes de traducir nada.
- [Textos más largos en español que no entran en botones o columnas de la
  interfaz WPF] → No se puede comprobar desde Linux. Se cubre con
  `--render-sample --lang es` en la verificación manual en Windows, y se acortan
  los textos que se corten.
- [La traducción de términos del juego (clases, efectos) no coincide con la del
  cliente oficial en español] → Se toman los nombres de clase del cliente
  español cuando se conocen. Los términos dudosos quedan listados en la tarea de
  revisión manual.

## Migration Plan

Sin migración: `AppSettings.Language` ya guarda el código. Quien tenga `en` o
`ru` sigue igual, y `es` pasa a ser un valor válido. Hacer rollback solo vuelve a
tratar `es` como inglés.
