#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <tag>"
    exit 1
fi

TAG="$1"

# Create tag and release
git tag "$TAG"
git push origin master "$TAG"
dotnet build -c Release


# Create release on github
gh release create "$TAG" --title "$TAG" --generate-notes
gh release upload "$TAG" PluginTemplate/bin/Release/BOCCHI/latest.zip --clobber

# Update plugin manifest
gh repo clone plugins
cd plugins
npm install

manifest_output=$(node generate_manifest.js)
commit_message=$(echo "$manifest_output" | awk '/^Suggested commit message:/{getline; print}')

git add manifest.json
git commit -m"$commit_message"
git push origin master

cd ..
rm -rf plugins
