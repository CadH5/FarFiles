<?php
include 'farfilesFuncs.inc';
session_start();
?>

<html>
<body onload="alert('Tests! But they DELETE and MESS UP the data, so be sure of the test environment, or close this page and do NOT press ok !!')">

<?php
$strDataFile = "./farfilesdata.dat";

$ipAddr = "36.234.0.55";
$ipInt = StrIpToInt($ipAddr);
echo "Test StrIpToInt($ipAddr): " . $ipInt . "<br>";
echo "Test GetStrIpFromInt($ipInt): '" . GetStrIpFromInt($ipInt) . "'<br>";
echo "<br>";

echo "Test wrong cmd:<br>";
echo "  resp='" . DoData("WRONGCMD", "Key", 1234, "10.10.10.10", $ipData) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

if (file_exists($strDataFile)) {
    echo "prev farfilesdata.dat:<br>";
    EchoDataFile("./farfilesdata.dat");
}

echo "Test delete, first server REGISTER:<br>";
ELib_FileDelete($strDataFile);
echo "  resp='" . DoData("REGISTER", "Key", 1234, "10.10.10.10", $ipData) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test first client REGISTER:<br>";
echo "  resp='" . DoData("REGISTER", "Key", 0, "0.0.0.0", $ipData) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test attempt server REGISTER with same key:<br>";
echo "  resp='" . DoData("REGISTER", "Key", 5678, "0.0.0.0", $ipData) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test server UNREGISTER:<br>";
echo "  resp='" . DoData("UNREGISTER", "Key", 9999, "0.0.0.0", $ipData) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test server REGISTER again:<br>";
echo "  resp='" . DoData("REGISTER", "Key", 1234, "10.10.10.10", $ipData) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "before cleanup:<br>";
EchoDataFile("./farfilesdata.dat");

CleanupDataFile("./farfilesdata.dat", "./farfilesdata_tmp.tmp");
echo "after cleanup:<br>";
EchoDataFile("./farfilesdata.dat");

?>
</body>
</html>

