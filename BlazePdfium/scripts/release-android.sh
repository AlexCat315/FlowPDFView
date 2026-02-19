#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GRADLE_PROPERTIES_FILE="$ROOT_DIR/gradle.properties"
BINDING_CSPROJ_FILE="$ROOT_DIR/../FlowPDFView.Android.Binding/Flow.PDFView.Android.Binding.csproj"

usage() {
  cat <<'USAGE'
Usage:
  ./scripts/release-android.sh [version]

Examples:
  ./scripts/release-android.sh
  ./scripts/release-android.sh 1.2.0

Behavior:
  1. Optionally updates blaze.version in BlazePdfium/gradle.properties
  2. Syncs Flow.PDFView.Android.Binding.csproj package version to blaze.version
  3. Builds release AARs for libPdfium and libPdfViewer
  4. Copies versioned AARs into FlowPDFView.Android.Binding/Jars
  5. Verifies copied AAR artifacts
USAGE
}

is_semver() {
  local value="$1"
  [[ "$value" =~ ^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$ ]]
}

update_version() {
  local next_version="$1"
  local tmp_file
  tmp_file="$(mktemp)"

  awk -v v="$next_version" '
    BEGIN { updated = 0 }
    /^blaze\.version=/ {
      print "blaze.version=" v
      updated = 1
      next
    }
    { print }
    END {
      if (!updated) {
        print "blaze.version=" v
      }
    }
  ' "$GRADLE_PROPERTIES_FILE" > "$tmp_file"

  mv "$tmp_file" "$GRADLE_PROPERTIES_FILE"
}

sync_binding_package_version() {
  local target_version="$1"
  local tmp_file
  tmp_file="$(mktemp)"

  if [[ ! -f "$BINDING_CSPROJ_FILE" ]]; then
    echo "Binding project file not found: $BINDING_CSPROJ_FILE" >&2
    exit 1
  fi

  awk -v v="$target_version" '
    BEGIN { in_property_group = 0; updated = 0 }
    /<PropertyGroup>/ { in_property_group = 1 }
    in_property_group && /<Version>[^<]*<\/Version>/ && !updated {
      sub(/<Version>[^<]*<\/Version>/, "<Version>" v "</Version>")
      updated = 1
    }
    in_property_group && /<\/PropertyGroup>/ && !updated {
      print "        <Version>" v "</Version>"
      updated = 1
      in_property_group = 0
    }
    { print }
    /<\/PropertyGroup>/ { in_property_group = 0 }
    END {
      if (!updated) {
        exit 1
      }
    }
  ' "$BINDING_CSPROJ_FILE" > "$tmp_file"

  mv "$tmp_file" "$BINDING_CSPROJ_FILE"
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

if [[ $# -gt 1 ]]; then
  usage
  exit 1
fi

if [[ $# -eq 1 ]]; then
  target_version="$1"
  if ! is_semver "$target_version"; then
    echo "Invalid version: $target_version" >&2
    echo "Expected semantic version format, e.g. 1.2.3" >&2
    exit 1
  fi

  update_version "$target_version"
  echo "Updated blaze.version=$target_version"
fi

current_version="$(awk -F= '/^blaze\.version=/{print $2}' "$GRADLE_PROPERTIES_FILE" | tail -n 1)"
if [[ -z "$current_version" ]]; then
  echo "Failed to resolve blaze.version from $GRADLE_PROPERTIES_FILE" >&2
  exit 1
fi

sync_binding_package_version "$current_version"
echo "Synced binding package version=$current_version"

cd "$ROOT_DIR"
./gradlew --no-daemon cleanBindingAars buildAll

echo "Release pipeline completed for blaze.version=$current_version"
