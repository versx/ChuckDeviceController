@echo off
SETLOCAL EnableExtensions EnableDelayedExpansion

:: Plugin file full path
SET targetPath=%~f1
:: Plugin project name
SET projectName=%2
:: Plugin file name with extension
SET targetFileName=%3
:: Plugin output directory
SET targetDir=%~f4
:: Root projects solution directory
SET solutionDir=%~f5
:: Plugin specific bin directory
SET pluginFolder=../../ChuckDeviceConfigurator/bin/Debug/plugins/%projectName%
:: Plugin dependencies config file
SET depsFileExt=.deps.json
:: Prefix of CDC libraries
SET cdcLib=ChuckDeviceController


if not exist "%pluginFolder%" (
  echo Creating plugin directory...
  mkdir "%pluginFolder%"
)

echo Copying 'appsettings.json' configuration file...
if not exist "%pluginFolder%/appsettings.json" if exist "%targetFolder/appsettings.json" (
  copy "%targetDir%/appsettings.json" "%pluginFolder%/appsettings.json"
)

echo Copying './Pages/*' Razor page files...
if exist "%targetDir%/Pages/" (
  xcopy /S /E /Y /I "%targetDir%/Pages" "%pluginFolder%/Pages"
)

echo Copying './Views/*' MVC view files...
if exist "%targetDir%/Views/" (
  xcopy /S /E /Y /I "%targetDir%/Views" "%pluginFolder%/Views"
)

echo Copying './wwwroot/*' static files...
if exist "%targetDir%/wwwroot/" (
  xcopy /S /E /Y /I "%targetDir%/wwwroot" "%pluginFolder%/wwwroot"
)

echo Copying all libraries from plugin bin folder...
for %%a in (%targetDir%*.dll) do (
  SET name=%%~nxa
  echo !name! | findstr /v /c:!cdcLib!>nul
  if !errorlevel!==0 (
    echo Copying library !name! to plugin folder...
    copy "%%a" "%pluginFolder%"
  )
)

echo Copying plugin dependencies config file '%projectName%.deps.json'...
copy "%targetDir%/%projectName%%depsFileExt%" "%pluginFolder%/%projectName%%depsFileExt%"

:: pause