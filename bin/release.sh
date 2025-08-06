#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <tag> [--testing]"
    exit 1
fi

TAG="$1"
IS_TESTING=false

if [ "$2" == "--testing" ]; then
    IS_TESTING=true
fi

echo "Tag: $TAG"
echo "Testing release: $IS_TESTING"

git tag "$TAG"
git push origin master "$TAG"

if [ "$IS_TESTING" = true ]; then
   gh release create "$TAG" --title "$TAG" --generate-notes --prerelease
else
   gh release create "$TAG" --title "$TAG" --generate-notes
fi
