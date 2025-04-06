<?php
header("Content-Type: application/json");

$request_body = file_get_contents("php://input");
$data = json_decode($request_body, true);
$errMsg = "";
$ipData = "";



// JWdP 20250223: not too much info for hackers
if (!isset($data['ConnectKey']) || !isset($data['UdpSvrPort']) || !isset($data['LocalIP']))
{
    $errMsg = "Incorrect input";
}

if ("" === $errMsg)
{
    $connectKey = $data['ConnectKey'];
    $udpSvrPort = $data['UdpSvrPort'];
    $localIP = $data['LocalIP'];

    $errMsg = GetData($connectKey, $udpSvrPort, $localIP, $ipData);
}

$response = ["status" => $errMsg === "" ? "success" : "error", "errMsg" => $errMsg, "ipData" => $ipData];

echo json_encode($response);


function GetData($connectKey, $udpSvrPort, $localIP, &$ipData)
{
    $ipData = "";
    $errMsg = "";
    $isForSvr = $udpSvrPort !== 0;
    $connectKeyUPC = strtoupper($connectKey);
    $strDataFile = "./farfilesdata.txt";

     if (!file_exists($strDataFile))
     {
         $fpTxt = fopen($strDataFile, "w");
         if ($fpTxt === false)
             return "Internal server error (GETDATA(1))";
        fclose($fpTxt);
     }

    $fpTxt = @fopen ($strDataFile, "r");
    if ($fpTxt === false)
        return "Internal server error (GETDATA(2))";

    while (($strLine = fgets($fpTxt)) !== FALSE)
    {
        $arrParts = explode(",", trim($strLine));
        $numParts = count($arrParts);
        if ($numParts < 4)
            continue;
        $lineConnectKey = $arrParts[0];
        if ($connectKeyUPC == strtoupper($lineConnectKey))
        {
            if ($isForSvr)
            {
                $errMsg = "ConnectKey already occupied for server";
            }
            else
            {
                $ipData = $arrParts[1] . "," . $arrParts[2] . "," . $arrParts[3];
            }
            break;
        }
    }
    fclose($fpTxt);

    if (! $isForSvr && "" === $ipData)
        $errMsg = "No server found with this ConnectKey";
    if ("" !== $ipData || "" !== $errMsg)
        return $errMsg;

    //JEEWEE: THIS DOES NOT COUNT WITH MULTI-USER
    $fpTxt = @fopen ($strDataFile, "a");
    if ($fpTxt === false)
        return "Internal server error (GETDATA(3))";

    $ipData = $_SERVER['REMOTE_ADDR'] . "," . $udpSvrPort . "," . $localIP;

    fprintf($fpTxt, "%s\n", $connectKey . "," . $ipData);
    fclose($fpTxt);

    return $errMsg;
}

?>

