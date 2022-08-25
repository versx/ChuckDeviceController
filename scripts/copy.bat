:: Plugin file full path
SET targetPath="%~f1"
:: Plugin project name
SET projectName="%2"
:: Plugin file name with extension
SET targetFileName="%3"
:: Plugin output directory
SET targetDir="%~f4"
:: Root projects solution directory
SET solutionDir="%~f5"
:: Root plugins bin directory
SET pluginsBin=ChuckDeviceConfigurator/bin/debug/plugins
:: Plugin specific bin directory
SET pluginFolder=../../%pluginsBin%/%projectName%/
:: Plugin dependencies config file
SET depsFileExt=.deps.json

:: Copy plugin library and dependencies config file
copy "%targetPath%" "%pluginFolder%/%targetFileName%"
copy "%targetPath%" "%pluginFolder%/%projectName%%depsFileExt%"

:: TODO: Copy all dependency files other than ChuckDeviceController.* libraries

:: Copy Views and wwwroot static file folders
if exist "%targetDir%/Views/" (
  xcopy /S /E /Y /I "%targetDir%/Views" "%pluginFolder%/Views"
)
if exist "%targetDir%/wwwroot/" (
  xcopy /S /E /Y /I "%targetDir%/wwwroot" "%pluginFolder%/wwwroot"
)

::"../../../scripts/copy.bat" "$(TargetPath)" "$(ProjectName)" "$(TargetFileName)" "$(TargetDir)" "$(SolutionDir)"