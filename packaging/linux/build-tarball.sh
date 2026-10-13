#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
dist="${1:-$root/dist}"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props")"
if [[ -z "$version" ]]; then
    echo "error: no <Version> in $root/Directory.Build.props" >&2
    exit 1
fi

name="beyond-meter-v$version-linux-x64"
stage="$dist/$name"
rm -rf "$stage"
mkdir -p "$stage/service"

dotnet publish "$root/src/AionMeter.Overlay/AionMeter.Overlay.csproj" -c Release -f net10.0 -r linux-x64 --self-contained true \
    -p:DebugType=none -p:DebugSymbols=false -o "$stage/app"
if [[ ! -f "$stage/app/data/npcs/en.json" ]]; then
    echo "error: data folder missing from the publish output" >&2
    exit 1
fi

cp "$root/LICENSE" "$stage/"
install -m 0755 "$root/packaging/linux/install.sh" "$stage/install.sh"
cp "$root/packaging/linux/aionmeter-overlay.service" "$stage/service/"
install -m 0755 "$root/packaging/linux/install-service.sh" "$stage/service/install-service.sh"

tar -C "$dist" -czf "$dist/$name.tar.gz" "$name"
cp "$dist/$name.tar.gz" "$dist/beyond-meter-linux-x64.tar.gz"
rm -rf "$stage"
echo "$dist/$name.tar.gz"
