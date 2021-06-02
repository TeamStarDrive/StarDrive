$solutiondir=$Args[0] # "C:\Projects\BlackBox\"
$targetdir=$Args[1] # "C:\Projects\BlackBox\UnitTests\bin\Release\"
$copymods=$Args[2]

$contentSrc = $solutiondir + "Content"
$contentDst = $targetdir + "Content"
echo "Copying $contentSrc to $contentDst"
# RoboCopy "source" "destination" /options...
# /e=recursive /xo=eclude-older-files(copy-if-newer)
# /NFL=no-filename-logs /NDL=no-dirname-logs /NJH=no-job-header
# /nc=no-fileclass-logs /ns=no-filesize-logs /np=no-progress
# /MT:16=multi-threaded-copy,16-threads
robocopy "$contentSrc" "$contentDst" /e /xo  /NFL /NDL /NJH /nc /ns /NP /MT:16

if ($copymods -eq "COPYMODS")
{
    $modsSrc = $solutiondir + "Mods"
    $modsDst = $targetdir + "Mods"
    if (Test-Path -Path "$modsSrc")
    {
        echo "Copying $modsSrc to $modsDst"
        robocopy "$modsSrc" "$modsDst" /e /xo  /NFL /NDL /NJH /nc /ns /np /MT:16
    }
}
