echo %1deploy\TortoiseHg\hg.exe
(%1deploy\TortoiseHg\hg.exe id -n|| echo +) | find "+" && exit 0

for /f %%r in ('%1deploy\TortoiseHg\hg.exe id -n -r .') do set hgrev=%%r
for /f "delims=" %%b in ('%1/deploy\TortoiseHg\hg id -b') do set hgbranch=%%b

%1/deploy/config/xml ed -L --update //add[@key='Version']/@value  -v "%hgrev%" %1\app.config
%1/deploy/config/xml ed -L --update //add[@key='Branch']/@value  -v "%hgbranch%" %1\app.config