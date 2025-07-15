#!/bin/bash

# Exit on any error
set -e

# Ensure a project name is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <NewProjectName>"
    exit 1
fi

NEW_NAME="$1"

# Rename the folder and related files
mv PluginTemplate "$NEW_NAME"
mv "$NEW_NAME/PluginTemplate.csproj" "$NEW_NAME/$NEW_NAME.csproj"
mv "$NEW_NAME/PluginTemplate.json" "$NEW_NAME/$NEW_NAME.json"
mv PluginTemplate.sln "$NEW_NAME.sln"

# Replace all instances of "PluginTemplate" with the new project name in all files within the new project folder
find "$NEW_NAME" -type f -exec sed -i "s/PluginTemplate/$NEW_NAME/g" {} +

# Add the project to the solution and build
dotnet sln "$NEW_NAME.sln" add "$NEW_NAME/$NEW_NAME.csproj"
dotnet build "$NEW_NAME.sln"
