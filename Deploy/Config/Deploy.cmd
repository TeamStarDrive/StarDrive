rem %1  =$([System.DateTime]::Now.ToString(`MMdd`))
if not exist ..\Config exit 0
set year=%date:~-2%
set month=%date:~4,2%
set day=%date:~7,2%

for /f %%r in ('hg id -n -r .') do set hgrev=%%r

for /f "delims=" %%i in (..\..\.hg\branch) do (
set name=%%i
)
set name=%name:/=_%
if not exist "..\config\config_%name%" (
copy "..\config\config.txt" "..\config\config_%name%" 
copy "..\config\include.txt" "..\config\include_%name%" 
)
echo %name%
echo ..\7-Zip\7z A sd.7z @"..\Config\include_%name%"
echo copy /b "..\7-Zip\7ZSD.sfx" + "..\Config\config_%name%" + sd.7z "%name% %hgrev%.exe"

..\7-Zip\7z A sd.7z @"..\Config\include_%name%"
copy /b "..\7-Zip\7ZSD.sfx" + "..\Config\config_%name%" + sd.7z "%name% %hgrev%.exe"
)

