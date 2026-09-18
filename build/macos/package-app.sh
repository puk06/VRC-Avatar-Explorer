#!/bin/bash
# Package an existing build/publish directory; never run another build here.
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: $0 OUTPUT_DIRECTORY [PUBLISH_DIRECTORY_TO_EXCLUDE]" >&2
    exit 1
fi

script_dir="$(cd "$(dirname "$0")" && pwd -P)"
repo_dir="$(cd "$script_dir/../.." && pwd -P)"
source_dir="$(cd "$1" && pwd -P)"
app_name="VRC-Avatar-Explorer.app"
destination="$source_dir/$app_name"

if [[ ! -f "$source_dir/AvatarExplorer" ]]; then
    echo "Missing macOS app host: $source_dir/AvatarExplorer" >&2
    exit 1
fi
if ! /usr/bin/file -b "$source_dir/AvatarExplorer" | /usr/bin/grep -q 'Mach-O'; then
    echo "App host is not a macOS executable: $source_dir/AvatarExplorer" >&2
    exit 1
fi
for required in LICENSE THIRD_PARTY_LICENSES.md; do
    if [[ ! -s "$source_dir/$required" ]]; then
        echo "Missing application resource: $source_dir/$required" >&2
        exit 1
    fi
done
for locale in "$repo_dir/AvatarExplorer.Core/Data/Localization/"*.json; do
    if [[ ! -s "$source_dir/locales/${locale##*/}" ]]; then
        echo "Missing localization resource: $source_dir/locales/${locale##*/}" >&2
        exit 1
    fi
done
# Only replace the bundle this script owns, not a symlink or unrelated directory.
if [[ -L "$destination" || ( -e "$destination" && ! -f "$destination/Contents/Info.plist" ) ]]; then
    echo "Refusing to replace unexpected bundle path: $destination" >&2
    exit 1
fi
if [[ -e "$destination" ]] && [[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$destination/Contents/Info.plist")" != 'com.github.puk06.vrc-avatar-explorer' ]]; then
    echo "Refusing to replace a different application: $destination" >&2
    exit 1
fi

version="$(sed -nE 's/^[[:space:]]*public static readonly string CurrentVersion = "([^"]+)";.*/\1/p' "$repo_dir/AvatarExplorer.Core/Services/System/AvatarExplorerApp.cs")"
if [[ ! "$version" =~ ^([0-9]+\.[0-9]+\.[0-9]+)(-beta\.[0-9]+)?$ ]]; then
    echo "Unrecognized application version: $version" >&2
    exit 1
fi
numeric_version="${BASH_REMATCH[1]}"

staging="$(mktemp -d "${TMPDIR:-/tmp}/avatar-explorer-bundle.XXXXXX")"
trap 'rm -rf "$staging"' EXIT
bundle="$staging/$app_name"
mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Resources" "$bundle/Contents/Frameworks"

# Exclude prior bundles and sibling RID/publish outputs. A generic build folder
# can already contain osx-arm64/ after a RID-specific publish.
excludes=(--exclude='*.app' --exclude='/publish/' --exclude='/osx-*/' --exclude='/win-*/' --exclude='/linux-*/')
if [[ -n "${2:-}" && -d "$2" ]]; then
    publish_dir="$(cd "$2" && pwd -P)"
    if [[ "$publish_dir" == "$source_dir/"* ]]; then
        excludes+=("--exclude=/${publish_dir#"$source_dir/"}/")
    fi
fi
/usr/bin/rsync -a "${excludes[@]}" "$source_dir/" "$bundle/Contents/Resources/"
mv "$bundle/Contents/Resources/AvatarExplorer" "$bundle/Contents/MacOS/AvatarExplorer"
# Keep data directories out of MacOS, where codesign treats directories as
# nested code. Relative links preserve AppContext.BaseDirectory lookups and
# .NET's runtime-specific native asset paths without changing application code.
while IFS= read -r -d '' resource; do
    name="${resource##*/}"
    if [[ "$name" == *.dylib ]]; then
        mv "$resource" "$bundle/Contents/Frameworks/$name"
        ln -s "../Frameworks/$name" "$bundle/Contents/MacOS/$name"
    else
        ln -s "../Resources/$name" "$bundle/Contents/MacOS/$name"
    fi
done < <(/usr/bin/find "$bundle/Contents/Resources" -mindepth 1 -maxdepth 1 -print0)
chmod +x "$bundle/Contents/MacOS/AvatarExplorer"

iconset="$staging/SoftwareIcon.iconset"
mkdir -p "$iconset"
for size in 16 32 128 256 512; do
    /usr/bin/sips -z "$size" "$size" "$repo_dir/AvatarExplorer.UI/Assets/Internal/SoftwareIcon.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double_size=$((size * 2))
    /usr/bin/sips -z "$double_size" "$double_size" "$repo_dir/AvatarExplorer.UI/Assets/Internal/SoftwareIcon.png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
/usr/bin/iconutil -c icns "$iconset" -o "$bundle/Contents/Resources/SoftwareIcon.icns"

cat > "$bundle/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key><string>com.github.puk06.vrc-avatar-explorer</string>
    <key>CFBundleName</key><string>VRC-Avatar-Explorer</string>
    <key>CFBundleDisplayName</key><string>VRC-Avatar-Explorer</string>
    <key>CFBundleExecutable</key><string>AvatarExplorer</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
    <key>CFBundleShortVersionString</key><string>$numeric_version</string>
    <key>CFBundleVersion</key><string>$numeric_version</string>
    <key>CFBundleGetInfoString</key><string>VRC-Avatar-Explorer $version</string>
    <key>CFBundleIconFile</key><string>SoftwareIcon.icns</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
EOF
/usr/bin/plutil -lint "$bundle/Contents/Info.plist"

# Sign nested native code explicitly, then the outer bundle. No certificate or
# hardened runtime is required for this local ad-hoc signature.
while IFS= read -r -d '' binary; do
    if /usr/bin/file -b "$binary" | /usr/bin/grep -q 'Mach-O'; then
        /usr/bin/codesign --force --sign - "$binary"
    fi
done < <(/usr/bin/find "$bundle/Contents/Resources" "$bundle/Contents/Frameworks" -type f -print0)
/usr/bin/codesign --force --sign - "$bundle"
/usr/bin/codesign --verify --deep --strict "$bundle"

if [[ -d "$destination" ]]; then
    rm -rf "$destination"
fi
/usr/bin/ditto "$bundle" "$destination"
echo "macOS app bundle -> $destination"
