#!/bin/bash

# Check if root directory is provided
if [ $# -eq 0 ]; then
    echo "Error: Root directory path is required"
    echo "Usage: $0 <root_directory_path>"
    exit 1
fi

ROOT_DIR="$1"
SCRIPT_DIR="$(dirname "$0")/requirements"
TARGET_SCRIPTS_DIR="${ROOT_DIR}/Assets/Scripts"

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo "Error: jq is not installed. Please install it first."
    echo "On macOS, you can install it using: brew install jq"
    exit 1
fi

MANIFEST_FILE="${ROOT_DIR}/Packages/manifest.json"

# Check if root directory exists
if [ ! -d "$ROOT_DIR" ]; then
    echo "Error: Root directory '$ROOT_DIR' does not exist"
    exit 1
fi

# Check and create Scripts directory if it doesn't exist
if [ ! -d "$TARGET_SCRIPTS_DIR" ]; then
    echo "Creating Scripts directory at $TARGET_SCRIPTS_DIR"
    mkdir -p "$TARGET_SCRIPTS_DIR"
fi

# Check if source scripts directory exists
if [ ! -d "$SCRIPT_DIR" ]; then
    echo "Error: Source scripts directory '$SCRIPT_DIR' does not exist"
    exit 1
fi

# Copy script files to target directory
echo "Copying script files to $TARGET_SCRIPTS_DIR"
cp -n "$SCRIPT_DIR"/*.cs "$TARGET_SCRIPTS_DIR/" 2>/dev/null || true
cp -n "$SCRIPT_DIR"/*.json "$TARGET_SCRIPTS_DIR/" 2>/dev/null || true

echo "Script files have been copied to $TARGET_SCRIPTS_DIR"

# Additional logic for XR Device Simulator folder
TARGET_XR_SIMULATOR_DIR="${ROOT_DIR}/Assets/Samples/XR Interaction Toolkit/3.1.1/XR Device Simulator"
SOURCE_XR_SIMULATOR_DIR="${SCRIPT_DIR}/XR Device Simulator"

if [ -d "${ROOT_DIR}/Assets/Samples/XR Interaction Toolkit/3.1.1" ]; then
    if [ ! -d "$TARGET_XR_SIMULATOR_DIR" ]; then
        echo "Copying XR Device Simulator folder to $TARGET_XR_SIMULATOR_DIR"
        cp -R "$SOURCE_XR_SIMULATOR_DIR" "$TARGET_XR_SIMULATOR_DIR"
        echo "XR Device Simulator folder has been copied."
    else
        echo "XR Device Simulator folder already exists at $TARGET_XR_SIMULATOR_DIR. Skipping copy."
    fi
else
    echo "Target directory /Assets/Samples/XR Interaction Toolkit/3.1.1 does not exist. Skipping XR Device Simulator copy."
fi

# Check and add packages if needed
PACKAGES=(
    "com.unity.xr.interaction.toolkit:3.1.1"
    "com.unity.nuget.newtonsoft-json:3.2.1"
)

modified_manifest=false

# Check if manifest file exists
if [ ! -f "$MANIFEST_FILE" ]; then
    echo "Error: manifest.json not found at $MANIFEST_FILE"
    exit 1
fi

# Function to check if a package exists with specific version in manifest
check_package() {
    local package=$1
    local version=$2
    jq -e --arg pkg "$package" --arg ver "$version" '.dependencies[$pkg] == $ver' "$MANIFEST_FILE" > /dev/null
    return $?
}

# Function to add a package to manifest
add_package() {
    local package=$1
    local version=$2
    jq --arg pkg "$package" --arg ver "$version" '.dependencies[$pkg] = $ver' "$MANIFEST_FILE" > "${MANIFEST_FILE}.tmp"
    mv "${MANIFEST_FILE}.tmp" "$MANIFEST_FILE"
}

# Check and update packages in manifest.json
echo "Checking packages in manifest.json..."
for pkg_info in "${PACKAGES[@]}"; do
    IFS=':' read -r package version <<< "$pkg_info"
    
    if ! check_package "$package" "$version"; then
        echo "Adding package to manifest.json: $package version $version"
        add_package "$package" "$version"
        modified_manifest=true
    else
        echo "Package $package version $version already exists in manifest.json"
    fi
done

if [ "$modified_manifest" = true ]; then
    echo "Manifest file has been updated."
else
    echo "No changes were needed. All required packages are present in manifest.json."
fi
