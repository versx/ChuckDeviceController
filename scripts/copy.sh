#!/bin/bash

# Plugin file full path
targetPath=$1
# Plugin project name
projectName=$2
# Plugin file name with extension
targetFileName=$3
# Plugin output directory
targetDir=$4
# Root projects solution directory
solutionDir=$5
# Plugin specific bin directory
pluginFolder=../../ChuckDeviceConfigurator/bin/Debug/plugins/$projectName
# Plugin dependencies config file
depsFileExt=.deps.json
# Prefix of CDC libraries
cdcLib=ChuckDeviceController.


echo "Creating plugin directory..."
mkdir -p "$pluginFolder"

echo "Copying 'appsettings.json' configuration file..."
if [[ ( ! -f "$pluginFolder/appsettings.json" ) && ( -f $"$targetDir/appsettings.json" ) ]]; then
  cp "$targetDir/appsettings.json" "$pluginFolder/appsettings.json"
fi

echo "Copying './Pages/*' Razor page files..."
if [ -d "$targetDir/Pages/" ]; then
  cp -R "$targetDir/Pages" "$pluginFolder/Pages"
fi

echo "Copying './Views/*' MVC view files..."
if [ -d "$targetDir/Views/" ]; then
  cp -R "$targetDir/Views" "$pluginFolder/Views"
fi

echo "Copying './wwwroot/*' static files..."
if [ -d "$targetDir/wwwroot/" ]; then
  cp -R "$targetDir/wwwroot" "$pluginFolder/wwwroot"
fi

echo "Copying './runtimes/*' dependency runtime libraries..."
if [ -d "$targetDir/runtimes/" ]; then
  cp -R "$targetDir/runtimes" "$pluginFolder/runtimes"
fi

echo "Copying all libraries from plugin bin folder..."
for entry in "$targetDir"/*.dll; do
    if [[ ! $entry =~ "$cdcLib" ]]; then
        echo "Copying library '$(basename $entry)' to plugin folder..."
        cp $entry $pluginFolder
    fi
done

echo "Copying plugin dependencies config file '$projectName.deps.json'..."
cp "$targetDir/$projectName$depsFileExt" "$pluginFolder/$projectName$depsFileExt"

#$SHELL