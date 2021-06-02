$targetdir=$Args[0] # "C:\Projects\BlackBox\UnitTests\bin\Release\"
$solutiondir=$Args[1] # "C:\Projects\BlackBox\"

$contentSrc = $solutiondir + "Content"
$contentDst = $solutiondir + "StarDrive\Content"
echo "Copying StarDrive/Content from $contentSrc to $contentDst"
# RoboCopy
# /e=recursive /xo=eclude-older-files(copy-if-newer)
# /NFL=no-filename-logs /NDL=no-dirname-logs /NJH=no-job-header
# /nc=no-fileclass-logs /ns=no-filesize-logs /np=no-progress
# /MT:16=multi-threaded-copy,16-threads
robocopy "$contentSrc" "$contentDst" /e /xo  /NFL /NDL /NJH /nc /ns /NP /MT:16

$modsSrc = $solutiondir + "Mods"
$modsDst = $solutiondir + "StarDrive\Mods"
echo "Copying StarDrive/Mods from $modsSrc to $modsDst"
robocopy "$modsSrc" "$modsDst" /e /xo  /NFL /NDL /NJH /nc /ns /np /MT:16
