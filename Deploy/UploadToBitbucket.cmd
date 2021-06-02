@echo off
if not exist Deploy.cmd ( echo UploadToBitbucket.cmd working directory must be in BlackBox\Deploy, but was: && cd && goto error )
if not defined BB_UPLOAD_USER ( echo envvar BB_UPLOAD_USER undefined && goto error )
if not defined BB_UPLOAD_PASS ( echo envvar BB_UPLOAD_PASS undefined && goto error )

echo APPVEYOR_REPO_BRANCH=%APPVEYOR_REPO_BRANCH%
echo APPVEYOR_REPO_COMMIT=%APPVEYOR_REPO_COMMIT%
::git name-rev --name-only %APPVEYOR_REPO_COMMIT%
::for /f %%b in ('git name-rev --name-only HEAD') do set BRANCH_NAME=%%b
set AutoDeploy=0
if "%APPVEYOR_REPO_BRANCH%" EQU "develop" ( set AutoDeploy=1 )
if "%APPVEYOR_REPO_BRANCH:~0,5%" EQU "test/" ( set AutoDeploy=1 )
if %AutoDeploy% NEQ 1 ( echo Auto-Deploy is not enabled for this branch && goto :eof )

for /f %%r in ('dir /B /O-D C:\Projects\BlackBox\Deploy\upload') do set file_name=%%r
set file=C:/Projects/BlackBox/Deploy/upload/%file_name%
echo Bitbucket Upload: %file%

curl --silent --head --fail "https://bitbucket.org/codegremlins/stardrive-blackbox/downloads/%file_name%"
if %ERRORLEVEL% EQU 0 (
  echo "File has already been uploaded to BitBucket?"
  goto :eof
)

curl -X POST -u "%BB_UPLOAD_USER%:%BB_UPLOAD_PASS%" -F files=@"%file%" "https://api.bitbucket.org/2.0/repositories/codegremlins/stardrive-blackbox/downloads"
goto :eof

:error
  set %ERRORLEVEL%=-1
