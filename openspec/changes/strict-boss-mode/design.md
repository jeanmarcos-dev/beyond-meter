## Context

`CombatTracker.OnDamage` decide qué daño entra en una pelea. Con
`TargetMode.BossOnly` hoy aplica dos reglas: durante una pelea con jefe ignora el
daño a otros NPC, y si hay una pelea sin jefe y aparece un jefe, la cierra y
abre otra. Sin jefe en pelea, cualquier NPC abre una pelea. El overlay muestra en
vivo la pelea en curso o, si no hay, la última terminada (`Find(null)`); por eso
una pelea de mobs reemplaza al jefe en pantalla. El historial decide qué guardar
con `HistoryPolicy` a partir de cada pelea terminada, y la visibilidad automática
usa `LiveFight()`. Un NPC es jefe (`NpcInfo.IsBoss`) por los datos del juego, por
ser un muñeco o, si no figura en los datos, por tener 5 000 000 de HP máximo o
más.

## Goals / Non-Goals

**Goals:**

- Que con `BossOnly` solo existan peleas contra jefes, de modo que la pantalla,
  el historial y la visibilidad automática queden bien sin tocarlos.

**Non-Goals:**

- Cambiar cómo se reconoce a un jefe.
- Agregar un tercer modo o una opción aparte.
- Cambiar `HistoryPolicy` o las opciones de historial.

## Decisions

### Filtrar en la entrada del daño

En `OnDamage`, con `BossOnly`, el daño a un NPC que no es jefe se descarta antes
de abrir o tocar una pelea. Como todas las peleas de ese modo empiezan con un
jefe, no hace falta nada más: la última pelea terminada sigue en pantalla porque
no aparece una nueva, el historial solo recibe peleas con jefe y `LiveFight()`
no ve peleas de mobs. El daño recibido y la curación ya se cuentan solo dentro
de una pelea en curso, así que siguen la misma regla.

Alternativa descartada: abrir una pelea oculta con los mobs y mostrarla solo si
en ella aparece un jefe. Recuperaría los primeros golpes a un jefe que se
reconoce tarde, pero obliga a esconderla de la pantalla, de la lista de
segmentos, del historial y de la visibilidad automática, y a decidir qué hacer
con ella al cambiar de zona o de modo.

### Se mantiene el cierre de una pelea sin jefe al aparecer uno

La regla que cierra una pelea sin jefe cuando se golpea a un jefe queda. Con el
filtro nuevo solo actúa si la pelea sin jefe venía del modo `Todo`, por ejemplo
al cambiar de modo desde Ajustes, que no reinicia el medidor. Así el jefe nunca
se mezcla con mobs de antes.

### Textos

`MeasureBoss` cambia en los tres idiomas. El botón de la barra (`FilterBosses`) y
los valores del archivo de ajustes (`BossOnly`, `All`) no cambian, así que no
hay migración. Se corrige el comentario existente de `TargetMode.BossOnly`, que
dejaría de ser cierto.

## Risks / Trade-offs

- [Un jefe que se reconoce tarde pierde sus primeros golpes] → Casi siempre el
  servidor anuncia al NPC al entrar en vista y la caché lo recuerda tras
  reiniciar el medidor. Si no, el primer registro de HP con su máximo lo marca
  como jefe y desde ahí se cuenta. La barra de HP ya avisa cuánto quedó sin
  contar.
- [Quien usaba el modo predeterminado para medir mobs deja de ver números] → Se
  avisa en las notas de la versión y en la documentación; el modo `Todo` sigue a
  un clic en la barra.
- [Muñecos de entrenamiento] → Ya cuentan como jefe, así que siguen midiéndose
  con `Jefes`.

## Migration Plan

No hay datos que migrar. Las notas de la versión explican el cambio del modo
predeterminado. Para volver atrás basta revertir el filtro y el texto.
