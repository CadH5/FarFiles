<?php

$arrNameExts = $_FILES["filesToUpload"]["name"];
$arrTmpNameExts = $_FILES["filesToUpload"]["tmp_name"];
$arrErrs = $_FILES["filesToUpload"]["error"];
$numNameExts = count($arrNameExts);
$numTmpNameExts = count($arrTmpNameExts);
$numErrs = count($arrErrs);

if ($numNameExts != 1 || $numTmpNameExts != 1 || $numErrs != 1)
{
    http_response_code(500);
    echo "ERROR: upload: expected 1 file, got: numNameExts=$numNameExts, numTmpNameExts=$numTmpNameExts, numErrs=$numErrs";
    exit;
}
if ($arrErrs[0] != UPLOAD_ERR_OK)
{
    http_response_code(500);
    echo "PHP ERROR: upload: expected status UPLOAD_ERR_OK, got $arrErrs[0]";
    exit;
}

$strTarget_file = "msgs/" . $arrNameExts[0];
if (move_uploaded_file($arrTmpNameExts[0], $strTarget_file) !== TRUE)
{
    http_response_code(500);
    echo "PHP ERROR: upload: could not handle $strTarget_file";     // but actually already a PHP warning is auto set in response
    exit;
}




