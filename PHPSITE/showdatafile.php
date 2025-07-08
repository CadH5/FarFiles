<?php
include 'farfilesFuncs.inc';
session_start();
?>

<html>
<body>

<?php
WriteDataFileAsTxt("./farfilesdata.dat", "./farfilesdata_dump.txt");
$dtNow = new DateTime();
echo "written, " . $dtNow->format("Y-m-d H:i:s.u") . "<br>";
?>
</body>
</html>

