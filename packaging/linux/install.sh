#!/usr/bin/env bash
set -euo pipefail

source_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
target="$HOME/.local/share/aionmeter-overlay"
bin_dir="$HOME/.local/bin"
binary="$target/aionmeter-overlay"

if [[ ! -f "$source_dir/app/aionmeter-overlay" ]]; then
    echo "error: $source_dir/app/aionmeter-overlay not found; run this script from the extracted release folder" >&2
    exit 1
fi

mkdir -p "$(dirname "$target")" "$bin_dir"
rm -rf "$target.new"
cp -a "$source_dir/app" "$target.new"
rm -rf "$target"
mv "$target.new" "$target"
chmod +x "$binary"
ln -sfn "$binary" "$bin_dir/aionmeter-overlay"
echo "Installed to $target"

ldconfig_bin="$(command -v ldconfig || echo /sbin/ldconfig)"
if ! "$ldconfig_bin" -p 2>/dev/null | grep -q 'libpcap\.so'; then
    echo "warning: libpcap was not found; install it with your package manager (libpcap) or the overlay cannot capture" >&2
fi

grant="setcap cap_net_raw,cap_net_admin+eip $binary"
if [[ "${AIONMETER_NO_SETCAP:-0}" == "1" ]]; then
    echo "Capture permission skipped. Grant it with: sudo $grant"
elif command -v setcap >/dev/null 2>&1 || [[ -x /usr/sbin/setcap ]]; then
    echo "Granting capture without root (needs sudo): $grant"
    sudo "$(command -v setcap || echo /usr/sbin/setcap)" cap_net_raw,cap_net_admin+eip "$binary"
else
    echo "warning: setcap was not found; install libcap and run: sudo $grant" >&2
fi

case ":$PATH:" in
    *":$bin_dir:"*) ;;
    *) echo "warning: $bin_dir is not in PATH; add it to run aionmeter-overlay by name" >&2 ;;
esac

echo "Done. If the overlay is running, restart it: aionmeter-overlay quit && aionmeter-overlay show"
