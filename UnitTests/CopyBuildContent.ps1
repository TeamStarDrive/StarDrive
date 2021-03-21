$targetdir=$Args[0] # "C:\Projects\BlackBox\UnitTests\bin\Release\"
$solutiondir=$Args[1] # "C:\Projects\BlackBox\"
$source = $targetdir + "Content"
$destination = $solutiondir + "StarDrive\Content"
echo "Copying StarDrive/Content for Testing from $source to $destination"

# /e=recursive /xo=eclude-older-files(copy-if-newer)
robocopy "$source" "$destination" /e /xo  /NFL /NDL /NJH /nc /ns /np
