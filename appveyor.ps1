
$stardrive = "C:\Projects\BlackBox\StarDrive\StarDrive.exe"

if ( [System.IO.File]::Exists($stardrive) )
{
    Write-Output "Detected cached game files, skipping download."
}
else
{
    $url = "http://94.23.147.120/blackbox/StarDrive.zip"
    $zip = "C:\Projects\BlackBox\StarDrive.zip"
    $out = "C:\Projects\BlackBox\StarDrive"
	$start_time = Get-Date
	#Start-FileDownload $url
    Start-BitsTransfer -Source $url -Destination $zip
	Write-Output "Time taken: $((Get-Date).Subtract($start_time).TotalSeconds) second(s)"
	Write-Output "Unzipping to .\StarDrive\ ..."
	Expand-Archive $zip -DestinationPath $out
}
