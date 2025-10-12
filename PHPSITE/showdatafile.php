<?php
include 'farfilesFuncs.inc';
session_start();
?>

<html>
<body>

<?php
$strNameDumpFile = "./farfilesdata_dump.txt";
WriteDataFileAsTxt("./farfilesdata.dat", $strNameDumpFile);
$dtNow = new DateTime();
echo "written, " . $dtNow->format("Y-m-d H:i:s.u") . "<br>";
echo "<br>";
$fp = fopen($strNameDumpFile, "r");
while (!feof($fp))
{
    echo fgets($fp) . "<br>";
}
fclose($fp);
?>
</body>
</html>

