<?php
header("Content-Type: application/json");

include 'farfilesFuncs.inc';

$request_body = file_get_contents("php://input");
$data = json_decode($request_body, true);
$errMsg = "";
$ipData = "";



// JWdP 20250223: not too much info for hackers
$cmd = "";
$connectKey = $data['ConnectKey'];
$udpSvrPort = $data['UdpSvrPort'];
$localIP = $data['LocalIP'];

$errMsg = "";
if (!isset($data['ConnectKey']) || !isset($data['UdpSvrPort']) || !isset($data['LocalIP']))
{
    $errMsg = "Incorrect input";
}

if ("" === $errMsg)
{
    $cmd = $data['Cmd'];
    $connectKey = $data['ConnectKey'];
    $udpSvrPort = $data['UdpSvrPort'];
    $localIP = $data['LocalIP'];

    $errMsg = DoData($cmd, $connectKey, $udpSvrPort, $localIP, $ipData);
}

$response = ["status" => $errMsg === "" ? "success" : "error", "errMsg" => $errMsg, "ipData" => $ipData];

echo json_encode($response);






?>

