$Newfile = @()
$updatefile = $true
$ver = .\Deploy\TortoiseHg\hg.exe id -n -r .
$branch = .\Deploy\TortoiseHg\hg.exe id -b
$newline = "`[assembly`: AssemblyInformationalVersion(`"" + $branch + "_" + $ver + "`")`]"
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