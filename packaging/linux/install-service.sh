#!/usr/bin/env bash
set -euo pipefail

unit=aionmeter-overlay.service
source_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
target_dir="${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user"

install -D -m 0644 "$source_dir/$unit" "$target_dir/$unit"
systemctl --user daemon-reload
systemctl --user enable "$unit"
