
$stardrive = "C:\Projects\BlackBox\StarDrive\StarDrive.exe"

if ( [System.IO.File]::Exists($stardrive) )
{
    Write-Output "Detected cached game files, skipping download."
}
else
{
    $url = "https://cdn-33.anonfiles.com/xfG3K5Lfo0/9ff48d58-1597089408/StarDrive.zip"
    $zip = "C:\Projects\BlackBox\StarDrive.zip"
    $out = "C:\Projects\BlackBox\StarDrive"
    $start_time = Get-Date
    Write-Output "Downloading StarDrive.zip ... 2-3 minutes for 710MB"
    #Start-FileDownload $url
    Start-BitsTransfer -Source $url -Destination $zip
    Write-Output "Time taken: $((Get-Date).Subtract($start_time).TotalSeconds) second(s)"
    Write-Output "Unzipping to .\StarDrive\ ..."
    Expand-Archive $zip -DestinationPath $out
}
