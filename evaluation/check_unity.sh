#!/bin/bash

# Function to check Unity on macOS
check_mac_unity() {
    # Check common Unity installation paths on macOS
    UNITY_PATHS=(
        "/Applications/Unity/Hub/Editor"
        "/Applications/Unity"
        "$HOME/Applications/Unity/Hub/Editor"
    )

    for path in "${UNITY_PATHS[@]}"; do
        if [ -d "$path" ]; then
            echo "Unity found on macOS at: $path"
            # List installed versions
            if [ -d "$path" ]; then
                echo "Installed Unity versions:"
                ls -1 "$path" 2>/dev/null
            fi
            return 0
        fi
    done
    echo "Unity not found on macOS"
    return 1
}

# Function to check Unity on Windows
check_windows_unity() {
    # Check if we're on Windows
    if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
        # Common Unity installation paths on Windows
        UNITY_PATHS=(
            "/c/Program Files/Unity/Hub/Editor"
            "/c/Program Files (x86)/Unity/Hub/Editor"
            "/c/Program Files/Unity"
            "/c/Program Files (x86)/Unity"
        )

        for path in "${UNITY_PATHS[@]}"; do
            if [ -d "$path" ]; then
                echo "Unity found on Windows at: $path"
                # List installed versions
                if [ -d "$path" ]; then
                    echo "Installed Unity versions:"
                    ls -1 "$path" 2>/dev/null
                fi
                return 0
            fi
        done
        echo "Unity not found on Windows"
        return 1
    else
        echo "Not running on Windows"
        return 1
    fi
}

# Main script
echo "Checking for Unity installation..."

# Detect OS and run appropriate check
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    check_mac_unity
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
    # Windows
    check_windows_unity
else
    echo "Unsupported operating system: $OSTYPE"
    exit 1
fi
