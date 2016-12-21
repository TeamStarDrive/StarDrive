rem %1  =$([System.DateTime]::Now.ToString(`MMdd`))
if not exist ..\Config exit 0
set year=%date:~-2%
set month=%date:~4,2%
set day=%date:~7,2%


for /f "delims=" %%i in (..\..\.hg\branch) do (
if not exist "..\config\config_%%i" (
copy "..\config\config.txt" "..\config\config_%%i" 
copy "..\config\include.txt" "..\config\include_%%i" 
)
echo %%i
echo ..\7-Zip\7z A sd.7z @"..\Config\include_%%i"
echo copy /b "..\7-Zip\7ZSD.sfx" + "..\Config\config_%%i" + sd.7z "%%i %month%%day%.exe"

..\7-Zip\7z A sd.7z @"..\Config\include_%%i"
copy /b "..\7-Zip\7ZSD.sfx" + "..\Config\config_%%i" + sd.7z "%%i%month%%day%.exe"
)

