# Linux overlay

`aionmeter-overlay` is a native overlay for AION 2 running under Steam/Proton on Linux (X11 or XWayland). It uses
the same capture, combat and history code as the Windows app. Its interface is in English, Spanish or Russian (see
Languages); the labels below are the English ones.

Each player row and the skills header show the class icon (the same PNGs as the Windows app, from
`src/AionMeter.Overlay/Assets/Classes`). Your own row also shows your level (`Lv 45`) once the game identifies your
character; other players' levels are not decoded yet.

## Install

Requirements: an x64 distribution with libpcap (the package is usually called `libpcap`). .NET is not needed.

```bash
curl -LO https://github.com/jeanmarcos-dev/beyond-meter/releases/latest/download/beyond-meter-linux-x64.tar.gz
tar xzf beyond-meter-linux-x64.tar.gz
./beyond-meter-v*-linux-x64/install.sh
```

`install.sh` copies the overlay to `~/.local/share/aionmeter-overlay`, links `~/.local/bin/aionmeter-overlay` and runs
`sudo setcap cap_net_raw,cap_net_admin+eip` on it so that it captures without root. It warns when libpcap is missing
or `~/.local/bin` is not in `PATH`. To update, run the `install.sh` of a newer release: it replaces the program,
grants the capture permission again and leaves your preferences, fights and recordings alone. Restart a running
overlay afterwards with `aionmeter-overlay quit && aionmeter-overlay show`.

### Build from source

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download) and libpcap.

```bash
dotnet publish src/AionMeter.Overlay/AionMeter.Overlay.csproj -c Release -o ~/.local/share/aionmeter-overlay
sudo setcap cap_net_raw,cap_net_admin+eip ~/.local/share/aionmeter-overlay/aionmeter-overlay
ln -sfn ~/.local/share/aionmeter-overlay/aionmeter-overlay ~/.local/bin/aionmeter-overlay
```

`setcap` has to be repeated after every publish. `packaging/linux/build-tarball.sh [output folder]` builds the same
tarball as a release (default output: `dist/`).

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
| `history` | Show the overlay with the history panel open |
| `settings` | Open the Settings window (see Settings) |
| `demo` | Play a 45-second fake fight in the running overlay; it is never saved |
| `breakdown` | Open the fight breakdown window for your own character (see Fight breakdown) |
| `copy` / `copyself` | Copy the party line or your own line for the game chat (180 characters max) |
| `mode` | Switch between counting boss damage only (`Bosses`) and everything you hit (`All`) |
| `heal` | Switch the table between the damage view (`Damage`) and the healing view (`Healing`: HPS, total and share of the group, healers only); the chosen view is kept while browsing the history, and the copy lines stay damage-based |
| `reload` | Re-read `settings.json` and apply it without restarting |
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
original speed; `--fast` plays it as fast as it can be parsed. The header says `Replay` (`Replay finished` at the
end) and the last fight stays on screen. A replay saves nothing: no fights in the history (which starts empty),
no names cache, no boss timers, no recordings. A missing or unreadable file prints an error on stderr and exits with a
non-zero code before any window opens.

A replay never shares a process with live capture, and it does not send a command to a running overlay: if one is
already open, `--replay` refuses with a message and exit code 1. Close it first with `aionmeter-overlay quit`.
While a replay is open, `aionmeter-overlay quit` closes it.

`Replay capture file…` in the tray menu plays a recording inside the running overlay instead, as fast as it can be
parsed: the live capture pauses meanwhile and resumes when the recording ends, and nothing from the recording is saved.

The same recordings work with the CLI: `aionmeter-cli replay <file.pcap>`.

## History panel

The `History` button opens the list of saved fights. Each row shows date, result (`✔` kill, `✖` wipe, `•` other),
title, zone, duration, number of players, your DPS and your place (`#2`). If you were not in the fight it shows the
party DPS (`Party 12.0K/s`) and no place.

- `Bosses only` and `Kills only` can be combined; the count beside them reads `shown of total`. They are kept in
  memory only and reset when the overlay restarts.
- `◀` / `▶` and the `older` / `newer` commands walk only the fights that pass the active filters, and the header
  reads `History N/M` over that filtered list. Changing a filter while viewing a fight that no longer passes returns
  to the live view.
- `Delete` appears only while a saved fight is on screen. It turns into `Delete? Yes / No` inside the overlay; the
  question is dropped after 5 seconds without an answer. Confirming deletes the file from the fights directory (there
  is no recycle bin) and shows the next older fight that passes the filters, the newer one if there is none, or the
  live view.

## Fight breakdown

`Details` (beside `Copy` in the skill panel) and `aionmeter-overlay breakdown` open a separate window for one
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
| `Timeline` | DPS per second smoothed over 3 seconds for the player, and the rest of the party as thin lines in class colour; the axis spans the fight |
| `Rotation` | Casts in order, DoT ticks left out, sized by damage, a gold dot on crits; hover for skill and damage; only the visible casts are drawn |
| `Defense` | Damage taken, damage taken per second of fight, hits taken |

Saved fights keep at most 5000 casts per player, live ones 20 000.

## Visibility and autostart

With the game closed the overlay stays hidden; it shows when the game starts and hides when it closes, and the
process keeps running. With the game open, a locked overlay hides itself after `AutoHideSeconds` without combat and
shows again, without taking focus, when a fight starts. An unlocked overlay never hides itself. Hiding it with
`toggle` wins over the automatic show until you show it by hand or the game closes and opens again; this state is
not saved. Fights are captured and saved while the overlay is hidden.

`service/install-service.sh` in the release folder (`packaging/linux/install-service.sh` in the source) copies
`aionmeter-overlay.service` to `~/.config/systemd/user` and enables it, so the overlay starts hidden with the graphical session and restarts after a
crash. `quit` and the `✕` button end the process with code 0 and are not restarted. Check it with
`systemctl --user is-enabled aionmeter-overlay`.

## Layout and size

The `⚙` button in the header opens a menu to show or hide each part of the overlay: the ping, the fight's clock, the
total and group line, the status (live, history or replay), the boss HP bar, the next-boss line, each column (DPS,
damage, %, crit), each toolbar button and the shortcut hints. Hiding every button hides the toolbar. `More settings…`
at the end opens the Settings window. The choice is saved in `HiddenParts`; the ping and the boss bar keep using
`ShowPing` and `ShowBossPanel`.

Drag the left or right edge to change the width; the left edge moves the window so its right edge stays put. Drag
the bottom edge to add or remove player rows, one row at a time (empty rows show while dragging), or a bottom corner
to do both. The top edge moves the window. While locked, the menu and the edges are off.

## Ping

The header shows the game's ping next to the fight's clock, taken from the ping the client exchanges with the server
every ~10 s. On Windows the reply is timed with the same counter the game stamps it with. Under Proton the meter reads
`CLOCK_MONOTONIC_RAW`, which is what Wine's `QueryPerformanceCounter` runs on; if that ever disagrees with the game's
own timing it falls back to timing each request against its reply, and the first value can take ~20 s to appear.
`ShowPing` in the settings file (or *Settings → Overlay*) hides it.

## Boss respawn timers

The overlay follows the field bosses of the character's server from the boss list of the in-game map, bosses seen
alive or dead and kills it witnesses. `timers` (or the `Respawns` button) swaps the damage table for one row per
boss: a countdown with the respawn time, `Alive`, `Should be up` once the time has passed unconfirmed, or
`Unknown interval`. While unlocked each row can watch the boss, step its interval (`−` / `+`), mark it dead now
or remove it. With the panel closed, a line shows the next watched boss and its countdown.

A watched boss raises one desktop alert (`notify-send`, critical) with a sound (`canberra-gtk-play`)
`BossAlertMinutes` before it respawns; without those tools the panel still works. `--demo` does not touch the saved
timers and never notifies.

## Settings

`More settings…` in the `⚙` menu, `Settings…` in the tray menu and `aionmeter-overlay settings` open the Settings
window. It has the same sections as the Windows app: language, overlay, meter, history, capture, hotkeys, updates and
data. `Save` applies every change at once, without restarting; `Cancel` leaves the settings and the file untouched.
On Linux the network adapter list and the hotkey fields are hidden: the capture picks the game connection by itself,
and shortcuts belong to the desktop (bind keys to the commands above).

## Tray

When the desktop has a notification area (StatusNotifierItem, for example KDE, or GNOME with the AppIndicator
extension), the overlay shows an icon. A click shows or hides the overlay; its menu shows or hides it, toggles
click-through, restarts the meter, opens the history, the boss timers and Settings, switches the language, plays a
demo fight or a recording, tests the boss alert and quits. Without a notification area the overlay works the same,
with the commands.

## Languages

The interface is in English, Spanish or Russian. Switch it from the language button in the toolbar (`EN`, `ES`,
`RU`), the tray menu or Settings; it applies at once to the overlay, the breakdown, the boss timers and the tray.
Skill and monster names follow the interface language unless Settings sets them apart (`NamesLanguage`). A new
install starts in English; an install updated from an older version keeps Spanish, the language it showed before.

## Preferences

`~/.config/aionmeter/settings.json` (or `$XDG_CONFIG_HOME/aionmeter/settings.json`), the same format as the Windows
app. Every key is optional. A value out of range is clamped to the nearest limit; an unreadable value falls back to
its default without affecting the rest. Position, width, rows, lock state, hidden parts and the breakdown size are
written by the overlay itself; change the others in Settings, or edit the file and run `aionmeter-overlay reload`.

The first time the overlay starts without `settings.json`, it imports the old `overlay.json` once: position, width,
lock, rows, target mode, timeouts, history, opacities, alert, auto-hide and breakdown size keep their values, the
interface stays in Spanish and its `Language` becomes `NamesLanguage`. `overlay.json` is left as it was.

| Key | Default | Range / values | Meaning |
|---|---|---|---|
| `OverlayLeft`, `OverlayTop` | `80`, `160` | pixels | Window position |
| `OverlayWidth` | `460` | 280–2000 | Window width |
| `OverlayHeight` | `330` | 160–2000 | Window height of the old Windows app; the overlay sizes itself to its rows |
| `BackgroundOpacity` | `0.9` | 0.15–1 | Background opacity while unlocked |
| `LockedOpacity` | `0.55` | 0.15–1 | Background opacity while locked |
| `Locked` | `false` | `true` / `false` | Start locked (click-through) |
| `ClickThrough` | `false` | `true` / `false` | Click-through flag of the old Windows app; the overlay uses `Locked` |
| `OverlayVisible` | `true` | `true` / `false` | Whether you left the overlay shown or hidden; hidden stays hidden at start unless `ShowOnStart` is on |
| `AutoShow` | `true` | `true` / `false` | Show by itself when a boss fight or your own fight starts, and hide again after `AutoHideSeconds` |
| `AutoHideSeconds` | `60` | 0–600 | Seconds without combat before a locked overlay hides itself; `0` never hides it |
| `ShowOnStart` | `true` | `true` / `false` | Show the overlay when the meter or the game starts |
| `BossAlertMinutes` | `2` | 0–30 | Minutes before a watched boss respawns that the alert fires; `0` turns alerts off |
| `TargetMode` | `BossOnly` | `BossOnly`, `All` | Count boss damage only, or everything you hit |
| `BarsRelativeToTop` | `true` | `true` / `false` | The leader's bar is full and the rest are sized against it |
| `MaxRows` | `10` | 1–20 | Player rows |
| `RowSize` | `100` | 50–100 | Row size in percent |
| `ShowGear` | `true` | `true` / `false` | Show GS / CP after the names |
| `ShowBossPanel` | `true` | `true` / `false` | Show the boss HP bar |
| `ShowPing` | `true` | `true` / `false` | Show the game's ping in the header |
| `HiddenParts` | `[]` | names of parts | Parts hidden from the `⚙` menu: `Clock`, `Totals`, `Source`, `NextBoss`, `DpsColumn`, `DamageColumn`, `ShareColumn`, `CritColumn`, `LiveButton`, `BrowseButtons`, `HistoryButton`, `SkillsButton`, `RespawnsButton`, `ResetButton`, `ViewButton`, `ModeButton`, `CopyButton`, `LockButton`, `LanguageButton`, `Shortcuts` |
| `IdleTimeoutSec` | `10` | 3–120 | Seconds without damage that end a fight |
| `BossIdleTimeoutSec` | `30` | 5–300 | Same, while a boss is alive |
| `Language` | `en` | `en`, `es`, `ru` | Interface language |
| `NamesLanguage` | `null` | `de en es fr ja ko pt ru zh-Hans zh-Hant` | Skill and monster names; `null` follows `Language`; missing names stay in English |
| `DownloadIcons` | `true` | `true` / `false` | Download skill icons and boss portraits |
| `CheckUpdates` | `true` | `true` / `false` | Ask GitHub for new versions |
| `AutoInstallUpdates` | `true` | `true` / `false` | Install updates by themselves (installed Windows copies only) |
| `SkippedUpdate` | `null` | version | Version the update notice skips |
| `SaveHistory` | `true` | `true` / `false` | Save fights automatically |
| `HistoryMode` | `BossesAndLong` | `BossesOnly`, `BossesAndLong`, `All` | Which fights are saved |
| `HistoryMinSeconds` | `20` | 5–600 | Minimum length of a non-boss fight in `BossesAndLong` |
| `RecordPackets` | `false` | `true` / `false` | Record the game traffic from the start (see Recording and replay) |
| `CaptureDevice` | `null` | adapter name | Network adapter to capture from (Windows); `null` picks it automatically |
| `HotkeyToggleOverlay`, `HotkeyReset`, `HotkeyClickThrough`, `HotkeyTimers` | `Ctrl+Shift+D`, `Ctrl+Shift+R`, `Ctrl+Shift+L`, `Ctrl+Shift+T` | `Ctrl`, `Alt`, `Shift`, `Win` and a key | Global hotkeys (Windows) |
| `BreakdownWidth`, `BreakdownHeight` | `960`, `640` | 480–3000, 320–2000 | Size of the breakdown window; written when it closes |
| `LayoutVersion` | `4` | number | Format version, written by the app |

Fights shorter than 5 seconds are never saved. The default `HistoryMode` keeps bosses and fights of at least 20
seconds, so that `All` targets do not fill the history with single mobs.

## Files

| Path | Content |
|---|---|
| `~/.config/aionmeter/settings.json` | Preferences |
| `~/.config/aionmeter/overlay.json` | Preferences of versions before 2.0, imported once and left untouched |
| `~/.local/share/aionmeter/fights/` | Saved fights |
| `~/.local/share/aionmeter/captures/` | Recordings made with `record` |
| `~/.local/share/aionmeter/names-cache.json` | Player names seen recently |
| `~/.local/share/aionmeter/boss-timers.json` | Boss respawn timers, per server |
| `~/.local/share/aionmeter/field-boss-learned.json` | Boss slots and map blocks learned from the game |
