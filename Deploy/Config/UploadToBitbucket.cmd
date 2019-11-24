@echo off
if not exist ..\Config ( echo UploadToBitbucket.cmd working directory must be in BlackBox\Deploy\Config, but was: && cd && goto error )
if not defined BB_UPLOAD_USER ( echo envvar BB_UPLOAD_USER undefined && goto error )
if not defined BB_UPLOAD_PASS ( echo envvar BB_UPLOAD_PASS undefined && goto error )

for /f %%b in ('git name-rev --name-only HEAD') do set BRANCH_NAME=%%b
echo BRANCH_NAME=%BRANCH_NAME%
if "%BRANCH_NAME%" NEQ "develop" ( echo Auto-Deploy is only enabled for develop branch && goto :eof )

for /f %%r in ('dir /B /O-D C:\Projects\BlackBox\Deploy\upload') do set file=%%r
set file=C:/Projects/BlackBox/Deploy/upload/%file%
echo Bitbucket Upload: %file%

curl -X POST -u "%BB_UPLOAD_USER%:%BB_UPLOAD_PASS%" -F files=@"%file%" "https://api.bitbucket.org/2.0/repositories/CrunchyGremlin/stardrive-blackbox/downloads"
goto :eof

:error
  set %ERRORLEVEL%=-1
