#!/bin/bash
set -e

if ! command -v gh &> /dev/null; then
    echo "Error: GitHub CLI (gh) is not installed."
    exit 1
fi

echo "Fetching releases for the current repository..."

gh release list