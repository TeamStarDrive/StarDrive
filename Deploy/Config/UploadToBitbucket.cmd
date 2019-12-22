@echo off
if not exist ..\Config ( echo UploadToBitbucket.cmd working directory must be in BlackBox\Deploy\Config, but was: && cd && goto error )
if not defined BB_UPLOAD_USER ( echo envvar BB_UPLOAD_USER undefined && goto error )
if not defined BB_UPLOAD_PASS ( echo envvar BB_UPLOAD_PASS undefined && goto error )

echo APPVEYOR_REPO_BRANCH=%APPVEYOR_REPO_BRANCH%
echo APPVEYOR_REPO_COMMIT=%APPVEYOR_REPO_COMMIT%
::git name-rev --name-only %APPVEYOR_REPO_COMMIT%
::for /f %%b in ('git name-rev --name-only HEAD') do set BRANCH_NAME=%%b
if "%APPVEYOR_REPO_BRANCH%" NEQ "develop" ( echo Auto-Deploy is only enabled for develop branch && goto :eof )

for /f %%r in ('dir /B /O-D C:\Projects\BlackBox\Deploy\upload') do set file_name=%%r
set file=C:/Projects/BlackBox/Deploy/upload/%file_name%
echo Bitbucket Upload: %file%

curl --silent --head --fail "https://bitbucket.org/CrunchyGremlin/stardrive-blackbox/downloads/%file_name%"
if %ERRORLEVEL% EQ 0 (
  echo "File has already been uploaded to BitBucket?"
  goto :eof
)

curl -X POST -u "%BB_UPLOAD_USER%:%BB_UPLOAD_PASS%" -F files=@"%file%" "https://api.bitbucket.org/2.0/repositories/CrunchyGremlin/stardrive-blackbox/downloads"
goto :eof

:error
  set %ERRORLEVEL%=-1
