#!/usr/bin/env bash
# Fails when a package in the given folder holds a library dll without its XML documentation beside it.
# Usage: scripts/check-package-docs.sh <folder with .nupkg files> [expected package count]
set -euo pipefail

folder="${1:?folder with .nupkg files}"
expected="${2:-1}"

shopt -s nullglob
packages=("$folder"/*.nupkg)
if (( ${#packages[@]} < expected )); then
  echo "::error::Expected at least $expected packages in $folder, found ${#packages[@]}"
  exit 1
fi

missing=0
for package in "${packages[@]}"; do
  # Listed once into a variable: piping unzip into grep -q can end unzip with SIGPIPE, which pipefail reports as a miss.
  files=$(unzip -Z1 "$package")
  for dll in $(grep -E '^lib/.+\.dll$' <<< "$files" || true); do
    if ! grep -qxF "${dll%.dll}.xml" <<< "$files"; then
      echo "::error::$(basename "$package") has no ${dll%.dll}.xml"
      missing=1
    fi
  done
done

if (( missing )); then
  exit 1
fi
echo "Every library in the ${#packages[@]} packages ships its XML docs."
