<?php
include 'farfilesFuncs.inc';
session_start();
?>

<html>
<body>

<?php
if (strtolower(UriParamval($_SERVER['REQUEST_URI'], "confirm")) !== "yes")
{
    echo "Tests! But they DELETE and MESS UP the data, therefor you must use ?confirm=yes";
    die;
}



$strDataFile = "./farfilesdata.dat";

$ipAddr = "36.234.0.55";
$ipInt = StrIpToInt($ipAddr);
echo "Test StrIpToInt($ipAddr): " . $ipInt . "<br>";
echo "Test GetStrIpFromInt($ipInt): '" . GetStrIpFromInt($ipInt) . "'<br>";
echo "<br>";

echo "Test wrong cmd:<br>";
echo "  resp='" . DoData("", "WRONGCMD", "Key", 1234, "","10.10.10.10", 0, 0,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

if (file_exists($strDataFile)) {
    echo "prev farfilesdata.dat:<br>";
    EchoDataFile("./farfilesdata.dat");
}

echo "Test delete, first server REGISTER:<br>";
ELib_FileDelete($strDataFile);
echo "  resp='" . DoData("", "REGISTER", "Key", 1234, "", "10.10.10.10", 0, 2,
                $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test first client REGISTER:<br>";
echo "  resp='" . DoData("", "REGISTER", "Key", 9876, "", "0.1.2.3", 1, 2,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test cmd GETDATA, should result same:<br>";
echo "  resp='" . DoData("", "GETDATA", "Key", 0, "", "0.0.0.0", 0, -1,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test second client REGISTER with same key: should result in error<br>";
echo "  resp='" . DoData("", "REGISTER", "Key", 5432, "", "9.9.9.9", 1, 1,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test attempt server REGISTER with same key:<br>";
echo "  resp='" . DoData("", "REGISTER", "Key", 5678, "", "0.0.0.0", 0, 0,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test server UNREGISTER:<br>";
echo "  resp='" . DoData("", "UNREGISTER", "Key", 9999, "", "0.0.0.0", 0, -1,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test server REGISTER again, now with id instead of udpport:<br>";
echo "  resp='" . DoData("", "REGISTER", "Key", 0, "12345678901234567890123456789012", "10.10.10.10", 0, 2,
        $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "Test client REGISTER, with id instead of udpport:<br>";
echo "  resp='" . DoData("", "REGISTER", "Key", 0, "99945678901234567890123456789012", "10.10.10.10", 1, 2,
                $ipData, $registeredCode) . "'<br>";
echo "  ipData='" . $ipData . "'<br>";
echo "<br>";

echo "before cleanup farfilesdata.dat:<br>";
EchoDataFile("./farfilesdata.dat");

CleanupDataFile("./farfilesdata.dat", "./farfilesdata_tmp.tmp");
echo "after cleanup farfilesdata.dat:<br>";
EchoDataFile("./farfilesdata.dat");

echo "before cleanup msgs:<br>";
EchoMsgsDir();

CleanupMsgs($numDeleted);
echo "after cleanup msgs, num deleted = $numDeleted:<br>";
EchoMsgsDir();

?>
</body>
</html>

