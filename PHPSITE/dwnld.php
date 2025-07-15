<?php
$strNameExt = $_GET["filenameext"];
if (! isset($_GET["filenameext"]))
{
    http_response_code(400);
    echo "PHP ERROR: download: invalid request";
    exit;
}
$strDownloadFullFile = "msgs/" . $strNameExt;
if (! file_exists($strDownloadFullFile)) {
    http_response_code(404);
    echo "PHP ERROR: download: file not found: $strNameExt";
    exit;
}

header('Content-Description: File Transfer');
header("Content-Type: application/force-download");
header("Content-Length: " . filesize($strDownloadFullFile));
header("Content-Disposition: attachment; filename=\"" .
    $strNameExt . "\"");	// not fullfile!
header("Content-Transfer-Encoding: binary");
header('Content-type: application/octet-stream');
if (@readfile($strDownloadFullFile) === FALSE) {
    http_response_code(500);
    echo "PHP ERROR: download: could not read file: $strNameExt";
    exit;
}

// delete downloaded file, without checking if delete succeeded:
@unlink($strDownloadFullFile);

