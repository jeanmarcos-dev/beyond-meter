# Linux overlay

`aionmeter-overlay` is a native overlay for AION 2 running under Steam/Proton on Linux (X11 or XWayland). It uses
the same capture, combat and history code as the Windows app. Its interface text is in Spanish.

Each player row and the skills header show the class icon (the same PNGs as the Windows app, from
`src/AionMeter.App/Assets/Classes`). Your own row also shows your level (`Nv 45`) once the game identifies your
character; other players' levels are not decoded yet.

## Install

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download) and libpcap.

```bash
dotnet publish src/AionMeter.Overlay/AionMeter.Overlay.csproj -c Release -o ~/.local/share/aionmeter-overlay
sudo setcap cap_net_raw,cap_net_admin+eip ~/.local/share/aionmeter-overlay/aionmeter-overlay
ln -sfn ~/.local/share/aionmeter-overlay/aionmeter-overlay ~/.local/bin/aionmeter-overlay
```

`setcap` lets the overlay capture without root and has to be repeated after every publish.

## Commands

`aionmeter-overlay [command] [--demo] [--hidden] [--replay <file.pcap> [--fast]]` starts the overlay, or sends the command to the one already running. Bind the
commands to desktop shortcuts to use them while playing.

| Command | Effect |
|---|---|
| `show` | Start or show the overlay |
| `toggle` | Show or hide it (starts it if it is not running) |
| `lock` | Lock (click-through) or unlock (move, resize, buttons) |
| `reset` | Close the current fight and clear the view |
| `live` / `older` / `newer` | Back to the live fight, or step through saved fights |
| `skills` | Open or close your own skill panel |
| `timers` | Open or close the boss respawn panel (`Respawns` button) |
| `breakdown` | Open the fight breakdown window for your own character (see Fight breakdown) |
| `copy` / `copyself` | Copy the party line or your own line for the game chat (180 characters max) |
| `mode` | Switch between counting boss damage only (`Jefes`) and everything you hit (`Todo`) |
| `heal` | Switch the table between the damage view (`Daño`) and the healing view (`Curación`: HPS, total and share of the group, healers only); the chosen view is kept while browsing the history, and the copy lines stay damage-based |
| `reload` | Re-read `overlay.json` and apply it without restarting |
| `record` | Start or stop recording the game traffic to a `.pcap` (see Recording and replay) |
| `quit` | Close the overlay |

`--demo` runs a fake fight with a throw-away history. `--hidden` starts the process without showing the window.

## Recording and replay

`record` toggles the recording of the game traffic without closing the capture or losing the running fight. Each
recording is a new file, `~/.local/share/aionmeter/captures/aion2_yyyyMMdd_HHmmss.pcap` (`XDG_DATA_HOME` aware), and
contains only the packets of the game connections that the capture already filters, never the rest of the network.
While recording, the header shows `● REC`, locked or unlocked. `record` only works on live capture. Recordings can
weigh tens of MB per hour and are never deleted by the overlay.

`aionmeter-overlay --replay <file.pcap>` opens the overlay over a recording instead of the live capture, at the
original speed; `--fast` plays it as fast as it can be parsed. The header says `Repetición` (`Repetición terminada`
at the end) and the last fight stays on screen. A replay saves nothing: no fights in the history (which starts empty),
no names cache, no boss timers, no recordings. A missing or unreadable file prints an error on stderr and exits with a
non-zero code before any window opens.

A replay never shares a process with live capture, and it does not send a command to a running overlay: if one is
already open, `--replay` refuses with a message and exit code 1. Close it first with `aionmeter-overlay quit`.
While a replay is open, `aionmeter-overlay quit` closes it.

The same recordings work with the CLI: `aionmeter-cli replay <file.pcap>`.

## History panel

The `Historial` button opens the list of saved fights. Each row shows date, result (`✔` kill, `✖` wipe, `•` other),
title, zone, duration, number of players, your DPS and your place (`#2`). If you were not in the fight it shows the
party DPS (`Grupo 12.0K/s`) and no place.

- `Solo jefes` and `Solo kills` can be combined; the count beside them reads `shown de total`. They are kept in
  memory only and reset when the overlay restarts.
- `◀` / `▶` and the `older` / `newer` commands walk only the fights that pass the active filters, and the header
  reads `Historial N/M` over that filtered list. Changing a filter while viewing a fight that no longer passes returns
  to the live view.
- `Borrar` appears only while a saved fight is on screen. It turns into `¿Borrar? Sí / No` inside the overlay; the
  question is dropped after 5 seconds without an answer. Confirming deletes the file from the fights directory (there
  is no recycle bin) and shows the next older fight that passes the filters, the newer one if there is none, or the
  live view.

## Fight breakdown

`Ver detalle` (beside `Copiar` in the skill panel) and `aionmeter-overlay breakdown` open a separate window for one
player of the fight on screen: the command opens your own character, the button the player whose skills are shown.
It is an ordinary window: decorated, movable, resizable, never always-on-top, centred on the monitor of the overlay
the first time it opens and sized as it was last closed (`BreakdownWidth`, `BreakdownHeight`). Unlike the overlay it
takes focus when it opens, since it is a window to read, not to play behind. Opening it again replaces the previous one.

The window freezes the fight that was on screen when it opened; browsing the history in the overlay afterwards does not
change it. A live fight refreshes once per second; a saved fight never changes. The list on the left holds every player
by place, with the class colour and yourself outlined, and switches all tabs to the one you pick.

| Tab | Content |
|---|---|
| `Skills` | Every skill with hits, DoT ticks, damage, DPS, average, max, share and crit, back, front, perfect, heavy and multi rates; `-` where there were no hits to measure |
| `Línea de tiempo` | DPS per second smoothed over 3 seconds for the player, and the rest of the party as thin lines in class colour; the axis spans the fight |
| `Rotación` | Casts in order, DoT ticks left out, sized by damage, a gold dot on crits; hover for skill and damage; only the visible casts are drawn |
| `Defensa` | Damage taken, damage taken per second of fight, hits taken |

Saved fights keep at most 5000 casts per player, live ones 20 000.

## Visibility and autostart

With the game closed the overlay stays hidden; it shows when the game starts and hides when it closes, and the
process keeps running. With the game open, a locked overlay hides itself after `AutoHideSeconds` without combat and
shows again, without taking focus, when a fight starts. An unlocked overlay never hides itself. Hiding it with
`toggle` wins over the automatic show until you show it by hand or the game closes and opens again; this state is
not saved. Fights are captured and saved while the overlay is hidden.

`packaging/linux/install-service.sh` copies `packaging/linux/aionmeter-overlay.service` to
`~/.config/systemd/user` and enables it, so the overlay starts hidden with the graphical session and restarts after a
crash. `quit` and the `✕` button end the process with code 0 and are not restarted. Check it with
`systemctl --user is-enabled aionmeter-overlay`.

## Boss respawn timers

The overlay follows the field bosses of the character's server from the boss list of the in-game map, bosses seen
alive or dead and kills it witnesses. `timers` (or the `Respawns` button) swaps the damage table for one row per
boss: a countdown with the respawn time, `Vivo`, `Debería estar vivo` once the time has passed unconfirmed, or
`Intervalo desconocido`. While unlocked each row can watch the boss, step its interval (`−` / `+`), mark it dead now
or remove it. With the panel closed, a line shows the next watched boss and its countdown.

A watched boss raises one desktop alert (`notify-send`, critical) with a sound (`canberra-gtk-play`)
`BossAlertMinutes` before it respawns; without those tools the panel still works. `--demo` does not touch the saved
timers and never notifies.

## Preferences

`~/.config/aionmeter/overlay.json` (or `$XDG_CONFIG_HOME/aionmeter/overlay.json`). Every key is optional. A value out
of range is clamped to the nearest limit; an unreadable value falls back to its default without affecting the rest.
Position, width and lock state are written by the overlay itself; edit the others and run `aionmeter-overlay reload`.

| Key | Default | Range / values | Meaning |
|---|---|---|---|
| `X`, `Y` | `40`, `240` | pixels | Window position |
| `Width` | `420` | 280–2000 | Window width |
| `Locked` | `false` | `true` / `false` | Start locked |
| `MaxRows` | `12` | 1–20 | Player rows (applies on restart) |
| `TargetMode` | `BossOnly` | `BossOnly`, `All` | Count boss damage only, or everything you hit |
| `Language` | `en` | `de en es fr ja ko pt ru zh-Hans zh-Hant` | Skill and NPC names; missing names stay in English |
| `IdleTimeoutSec` | `10` | 3–120 | Seconds without damage that end a fight |
| `BossIdleTimeoutSec` | `30` | 5–300 | Same, while a boss is alive |
| `HistoryMode` | `BossesAndLong` | `BossesOnly`, `BossesAndLong`, `All` | Which fights are saved |
| `HistoryMinSeconds` | `20` | 5–600 | Minimum length of a non-boss fight in `BossesAndLong` |
| `LockedOpacity` | `0.55` | 0.15–1 | Background opacity while locked |
| `Opacity` | `0.85` | 0.15–1 | Background opacity while unlocked |
| `BossAlertMinutes` | `2` | 0–30 | Minutes before a watched boss respawns that the desktop alert fires; `0` turns alerts off |
| `AutoHideSeconds` | `60` | 0–600 | Seconds without combat before a locked overlay hides itself; `0` never hides it |
| `BreakdownWidth`, `BreakdownHeight` | `960`, `640` | 480–3000, 320–2000 | Size of the breakdown window; written when it closes |

Fights shorter than 5 seconds are never saved. Before `HistoryMode` existed every fight of 5 seconds or more was
saved; the default now keeps bosses and fights of at least 20 seconds, so that `All` targets do not fill the history
with single mobs.

## Files

| Path | Content |
|---|---|
| `~/.config/aionmeter/overlay.json` | Preferences |
| `~/.local/share/aionmeter/fights/` | Saved fights |
| `~/.local/share/aionmeter/captures/` | Recordings made with `record` |
| `~/.local/share/aionmeter/names-cache.json` | Player names seen recently |
| `~/.local/share/aionmeter/boss-timers.json` | Boss respawn timers, per server |
| `~/.local/share/aionmeter/field-boss-learned.json` | Boss slots and map blocks learned from the game |
