echo git show --oneline -s
git show --oneline -s || exit 0
cd
if not exist ..\Config exit 0
set year=%date:~-2%
set month=%date:~4,2%
set day=%date:~7,2%

:: Get the branch name and replace release/ with empty and / with _
for /f "delims=" %%b in ('git name-rev --name-only HEAD') do set name=%%b
set name=%name:release/=%
set name=%name:/=_%

:: Get current revision `7912`
for /f %%r in ('git rev-list --count HEAD') do set revision=%%r
echo Name=%name%
echo Revision=%revision%
set installer_filename=BlackBox_Mars_%name%_%revision%
echo %installer_filename% > version.txt

copy "%1deploy\config\config.txt" "%1deploy\config\config_%name%" 
copy "%1deploy\config\include.txt" "%1deploy\config\include_%name%" 

echo Packaging %installer_filename% to sd.7z
echo ..\7-Zip\7z A sd.7z @"%1deploy\Config\include_%name%"
echo copy /b "..\7-Zip\7ZSD.sfx" + "%1deploy\Config\config_%name%" + sd.7z "%installer_filename%.exe"

:: Always remove sd.7z otherwise we'll get nasty bugs / redundant files
echo Creating SFX %installer_filename%.exe
del /f sd.7z
if not exist ..\upload mkdir ..\upload
..\7-Zip\7z a sd.7z @"%1deploy\Config\include_%name%" > NUL
copy /b "..\7-Zip\7ZSD.sfx" + "%1deploy\Config\config_%name%" + sd.7z "../upload/%installer_filename%.exe"
echo Created %installer_filename%.exe
:: ..\7-Zip\7z a -sfx7zSD.sfx "%installer_filename%.exe" @"%1deploy\Config\include_%name%"
