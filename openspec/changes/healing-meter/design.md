# Design

## Context

`PacketParser` emite `HealEvent` (directo y por ticks). `CombatTracker.OnHeal`
resuelve el dueño con `ResolveOwner` y suma `Combatant.Healing` solo si ese
jugador ya es combatiente de la pelea activa, es decir, si ya hizo daño. Ni
`CombatantSnapshot` ni `FightRecord` llevan la curación.

## Goals / Non-Goals

**Goals:**
- Curación por jugador correcta para sanadores puros.
- Compatibilidad con historiales existentes en Linux y Windows.

**Non-Goals:**
- Desglose de curación por skill.
- Separar sobrecuración: se muestra la cantidad que informa el juego.

## Decisions

- **Alta de sanadores en `OnHeal`**: si el origen resuelto es un jugador (no
  NPC) y hay pelea activa, se crea su `Combatant` como en `OnDamage` pero sin
  tocar `FirstHitMs`/`LastDamageMs`, para que curar no alargue la pelea ni
  cambie su DPS. Alternativa descartada: una tabla de sanadores aparte, que
  obligaría a duplicar resolución de nombres y clases.
- **Solo curaciones hacia jugadores**: se ignoran las que tienen como destino
  un NPC, para no contar curas de jefes o de mobs.
- **Campos opcionales al final de `CombatantSnapshot`** (`long Healing = 0`,
  `double Hps = 0`): `System.Text.Json` deja el default cuando el JSON viejo no
  los trae, y la app de Windows compila sin cambios.
- **Porcentaje de curación** calculado en `MeterView` sobre la suma de la
  pelea, no persistido.
- **Filas de sanadores puros en la vista de daño**: aparecen con 0 al final;
  se aceptan para no esconder a un participante.

## Risks / Trade-offs

- [Un `HealEvent` mal atribuido podría inflar a un jugador] → los tests del
  parser existentes cubren el decodificado; se añaden casos de dueño de
  invocación y de destino NPC.
- [Cambiar `OnHeal` afecta a la app de Windows] → solo añade participantes con
  daño 0; se verifica que los tests de motor existentes siguen pasando.

## Open Questions

- Si el juego informa la curación bruta o la efectiva. No cambia el diseño:
  la vista mostrará lo que llegue y se puede etiquetar después.
