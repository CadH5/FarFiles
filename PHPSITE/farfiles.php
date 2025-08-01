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

if (!isset($data['Cmd']) || !isset($data['ConnectKey']) || !isset($data['UdpPort']) || !isset($data['LocalIP']) ||
    !isset($data['IsSvr0Client1']) || !isset($data['CommunicModeAsInt']))
{
    $errMsg = "Incorrect input";
}

if ("" === $errMsg)
{
    $cmd = $data['Cmd'];
    $connectKey = $data['ConnectKey'];
    $udpPort = $data['UdpPort'];
    $localIP = $data['LocalIP'];
    $isSvr0Client1 = $data['IsSvr0Client1'];
    $CommunicModeAsInt = $data['CommunicModeAsInt'];

    $errMsg = DoData($cmd, $connectKey, $udpPort, $localIP, $isSvr0Client1, $CommunicModeAsInt,
            $ipData);
}

$response = ["status" => $errMsg === "" ? "success" : "error", "errMsg" => $errMsg, "ipData" => $ipData];

echo json_encode($response);

?>

