
$stardrive = "C:\Projects\BlackBox\StarDrive\StarDrive.exe"

if ( [System.IO.File]::Exists($stardrive) )
{
    Write-Output "Detected cached game files, skipping download."
}
else
{
    $url = "https://filebin.net/jikeykt2f2njtbdd/StarDrive.zip?t=4gj9qmk6"
    $zip = "C:\Projects\BlackBox\StarDrive.zip"
    $out = "C:\Projects\BlackBox\StarDrive"
    $start_time = Get-Date
    Write-Output "Downloading StarDrive.zip ... 2-3 minutes for 707MB"
    #Start-FileDownload $url
    Start-BitsTransfer -Source $url -Destination $zip
    Write-Output "Time taken: $((Get-Date).Subtract($start_time).TotalSeconds) second(s)"
    Write-Output "Unzipping to .\StarDrive\ ..."
    Expand-Archive $zip -DestinationPath $out
}
