
$stardrive = "C:\Projects\BlackBox\StarDrive\StarDrive.exe"

if ( [System.IO.File]::Exists($stardrive) )
{
    Write-Output "Detected cached game files, skipping download."
}
else
{
    $url = "https://doc-0s-a0-docs.googleusercontent.com/docs/securesc/0f7c6kvf134giqc9npkhh3bncn54r12n/npd18spv7n53stc9uhs4pqsmcdrhrfcq/1597087425000/07190736467189418209/07190736467189418209/1DbVMaSyNAi6tuMGcV54SCWFkGyYAunje?e=download&authuser=0&nonce=67np9mj6k8nfa&user=07190736467189418209&hash=s79cn6hm14sa1t29sjd5n0cajnicp6am"
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
