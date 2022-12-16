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


:: TODO: Create plugin folder if does not exist
:: TODO: Copy all dependency files other than ChuckDeviceController.* libraries

if not exist "%pluginFolder%/appsettings.json" (
  xcopy /Y "%targetDir%/appsettings.json" "%pluginFolder%/appsettings.json"
)

:: Copy all directories from plugin build output folder
::xcopy /S /E /Y /I "%targetDir%/" "%pluginFolder%/"

if exist "%targetDir%/Pages/" (
  xcopy /S /E /Y /I "%targetDir%/Pages" "%pluginFolder%/Pages"
)
if exist "%targetDir%/Views/" (
  xcopy /S /E /Y /I "%targetDir%/Views" "%pluginFolder%/Views"
)
if exist "%targetDir%/wwwroot/" (
  xcopy /S /E /Y /I "%targetDir%/wwwroot" "%pluginFolder%/wwwroot"
)

:: Copy plugin library and dependencies config file
copy "%targetPath%" "%pluginFolder%/%targetFileName%"
copy "%targetDir%/%projectName%%depsFileExt%" "%pluginFolder%/%projectName%%depsFileExt%"

::"../../../scripts/copy.bat" "$(TargetPath)" "$(ProjectName)" "$(TargetFileName)" "$(TargetDir)" "$(SolutionDir)"