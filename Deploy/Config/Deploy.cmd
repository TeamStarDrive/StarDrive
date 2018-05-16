echo %1deploy\TortoiseHg\hg.exe
%1deploy\TortoiseHg\hg.exe id ||  exit 0
cd
if not exist ..\Config exit 0
set year=%date:~-2%
set month=%date:~4,2%
set day=%date:~7,2%

REM(Get current branch and tagdistance `release/Texas_A2_RC1_222`; then replace release/ and / with _)
for /f %%r in ('%1deploy\TortoiseHg\hg log -r tip --template {branch}_{latesttagdistance}') do set hgrev=%%r
set hgrev=%hgrev:release/=%
set hgrev=%hgrev:/=_%
echo %hgrev% > version.txt

REM(Get the branch name and replace / with _)
for /f "delims=" %%b in ('%1\deploy\TortoiseHg\hg id -b') do set name=%%b
set name=%name:/=_%

if not exist "%1deploy\config\include_%name%" (
copy "%1deploy\config\config.txt" "%1deploy\config\config_%name%" 
copy "%1deploy\config\include.txt" "%1deploy\config\include_%name%" 
)
echo %name%
echo ..\7-Zip\7z A sd.7z @"%1deploy\Config\include_%name%"
echo copy /b "..\7-Zip\7ZSD.sfx" + "%1deploy\Config\config_%name%" + sd.7z "%hgrev%.exe"

..\7-Zip\7z A sd.7z @"%1deploy\Config\include_%name%"
copy /b "..\7-Zip\7ZSD.sfx" + "%1deploy\Config\config_%name%" + sd.7z "%hgrev%.exe"
)

