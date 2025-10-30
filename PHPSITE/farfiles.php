<?php
header("Content-Type: application/json");

include 'farfilesFuncs.inc';

$request_body = file_get_contents("php://input");
$data = json_decode($request_body, true);
$errMsg = "";
$ipData = "";
$communicMode = "";


// JWdP 20250223: not too much info for hackers

$errMsg = "";
$strMonth3 = strtolower(substr(date("F"), 0, 3));
$strLogFileName = $strMonth3 . "-" . date("Y") . ".txt";

if (!isset($data['Cmd']) || !isset($data['ConnectKey']) ||
    (!isset($data['UdpPort']) && !isset($data['IdInsteadOfUdp'])) ||
    !isset($data['LocalIP']) ||
    !isset($data['IsSvr0Client1']) || !isset($data['CommunicModeAsInt']))
{
    $errMsg = "Incorrect input";
    TryLogAppend($strLogFileName, $errMsg);
}

if ("" === $errMsg)
{
    $cmd = $data['Cmd'];
    $connectKey = $data['ConnectKey'];
    $udpPort = isset($data['UdpPort']) ? intval($data['UdpPort']) : 0;
    $idInsteadOfUdp = $data['IdInsteadOfUdp'] ?? "";
    $localIP = $data['LocalIP'];
    $isSvr0Client1 = $data['IsSvr0Client1'];
    $CommunicModeAsInt = $data['CommunicModeAsInt'];

    TryLogAppend($strLogFileName, "incoming: " .
        "cmd=$cmd, connectKey=$connectKey, udpPort=$udpPort, idInsteadOfUdp=$idInsteadOfUdp, localIP=$localIP, isSvr0Client1=$isSvr0Client1, CommunicModeAsInt=$CommunicModeAsInt");

    if ($udpPort === 0 && strlen($idInsteadOfUdp) != 32)
        $errMsg = "Incorrect input(2)";
    else if ($udpPort !== 0 && strlen($idInsteadOfUdp) > 0)
        $errMsg = "Incorrect input(3)";
}

if ("" === $errMsg)
{
    $errMsg = DoData($cmd, $connectKey, $udpPort, $idInsteadOfUdp, $localIP, $isSvr0Client1, $CommunicModeAsInt,
            $ipData, $registeredCode);
}

// registeredCode: 0=none, 1=server, 2=server+client
$response = ["status" => $errMsg === "" ? "success" : "error", "errMsg" => $errMsg, "ipData" => $ipData,
    "registeredCode" => $registeredCode];

echo json_encode($response);

?>

