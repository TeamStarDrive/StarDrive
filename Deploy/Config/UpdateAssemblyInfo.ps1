$newfile = @()
$updatefile = $false


$name = .\Deploy\TortoiseHg\hg.exe id -b
$name = $name.Replace("release/", "")
$revision = .\Deploy\TortoiseHg\hg.exe log -r . --template `{rev`}
$ver = $name + "_" + $revision

$newline = "`[assembly`: AssemblyInformationalVersion(`"" + $ver + "`")`]"
Write-Host -ForegroundColor Yellow $ver " " $newline
Get-Content ".\Properties\AssemblyInfo.cs" | ForEach-Object {

    $currentline = $_

    if ($currentline -like "*AssemblyInformationalVersion*") 
    {
        if ($currentline.Split('"')[1] -ne $ver) {
            $updatefile = $true
        }
        $currentline = $newline
    }
    $newfile += $currentline
}

if ($updatefile) {Set-Content -Path ".\Properties\AssemblyInfo.cs" -Value $newfile -Force}
else {Write-Host -ForegroundColor Yellow "Not updating file, because version has not changed"}