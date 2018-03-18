$Newfile = @()
$updatefile = $true
$ver = .\Deploy\TortoiseHg\hg.exe log -r tip --template `{latesttag`}_`{latesttagdistance`}

$newline = "`[assembly`: AssemblyInformationalVersion(`"" + $ver + "`")`]"
Write-Host -ForegroundColor Yellow $ver " " $newline
Get-Content ".\Properties\AssemblyInfo.cs" | ForEach-Object {

    $currentline = $_

    if ($currentline -like "*AssemblyInformationalVersion*") 
    {
        if ($currentline.Split('"')[1] -eq $newline.Split('"')[1]) {$updatefile = $false}
        $currentline = $newline
    }
    $newfile += $currentline
}

if ($updatefile) {Set-Content -Path ".\Properties\AssemblyInfo.cs" -Value $Newfile -Force}
else {Write-Host -ForegroundColor Yellow "Not updating file, because version has not changed"}