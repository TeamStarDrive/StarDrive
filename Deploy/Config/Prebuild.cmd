hg id -n | find "+" && exit 0

for /f %%r in ('hg id -n -r .') do set hgrev=%%r
for /f "delims=" %%b in ('hg id -b') do set hgbranch=%%b

%1/deploy/config/xml ed -L --update //add[@key='Version']/@value  -v "%hgrev%" %1\app.config
%1/deploy/config/xml ed -L --update //add[@key='Branch']/@value  -v "%hgbranch%" %1\app.config