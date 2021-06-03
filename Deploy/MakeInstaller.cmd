@echo off
echo Deploy should only be done via AppVeyor
if /i not defined APPVEYOR_BUILD_VERSION set APPVEYOR_BUILD_VERSION=0.0.0.0

echo APPVEYOR_BUILD_VERSION=%APPVEYOR_BUILD_VERSION%

:: cd to given parameter if it exists
if not [%1]==[] (
    echo CurrentDir=%cd% ChangeDirTo=%1
    cd %1
)

:: Assume MakeInstaller.cmd is called from C:\Projects\BlackBox folder
if not exist "Deploy/NSIS/makensis.exe" (
    echo MakeInstaller.cmd must be executed with WorkingDir=Projects\BlackBox\
    exit /b %errorlevel%
)

set source=%cd%
if not exist Deploy\upload mkdir Deploy\upload

::echo Packaging Content files to sd.7z
::del /f Deploy\upload\sd.7z
::Deploy\7-Zip\7z.exe A Deploy\upload\sd.7z @"Deploy\include.txt"

echo "MakeNSIS Deploy/BlackBox-Mars.nsi"
"Deploy/NSIS/makensis.exe" /V3 /DVERSION=%APPVEYOR_BUILD_VERSION% /DSOURCE_DIR=%source% Deploy/BlackBox-Mars.nsi
