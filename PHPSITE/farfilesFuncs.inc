<?php

const UNREGISTERED_CONNKEY = "-";

function DoData($cmd, $connectKey, $udpPort, $localIP, $isSvr0Client1, &$ipData)
{
    if ($cmd !== "REGISTER" && $cmd !== "UNREGISTER" && $cmd !== "GETDATA")
        return "Internal server error (GETDATA(CMD))";

    $ipData = "";
    $errMsg = "";
    //JEEWEE
    //$isForSvr = $udpPort !== 0;
    $ipPubl = $_SERVER['REMOTE_ADDR'];
    // for some reason, if I use localhost and xampp, $_SERVER['REMOTE_ADDR'] results in "::1"
    if (strlen($ipPubl) > 2 && substr($ipPubl, 0, 2) === "::") {
        $ipPubl = "0.0.0.0";
    }
    $connectKeyUPC = strtoupper($connectKey);
    $strDataFile = "./farfilesdata.dat";
    $strDataFileTempCleanup = "./farfilesdata_tempCleanup.tmp";
    $fpData = null;
    $nUnregisteredCounter = 0;
    $bUnregistered = FALSE;

    try
    {
        BlockDeblock(TRUE);

        // create empty file if this is first time
        if (!file_exists($strDataFile)) {
            $fpData = fopen($strDataFile, "wb");
            if ($fpData === false)
                $errMsg = "Internal server error (GETDATA(1))";
            else
                fclose($fpData);
        }

        if ("" === $errMsg) {
            $fpData = @fopen($strDataFile, "r+b");
            if ($fpData === false)
                $errMsg = "Internal server error (GETDATA(2))";
        }

        if ("" === $errMsg) {
            $prevPos = 0;
            while (($oneData = ReadOneData($fpData)) !== FALSE) {
                if (! IsRegistrationStillValid($oneData))
                {
                    $nUnregisteredCounter++;
                }
                else {
                    if ($connectKeyUPC == strtoupper($oneData->strConnKey)) {

                        // this connectKey exists in file and is not unregistered
                        $ipData = $oneData->ToIpData();

                        if ($cmd === "GETDATA") {
                            break;
                        }
                        if ($isSvr0Client1 === 0) {
                            if ($cmd === "UNREGISTER") {
                                $oneData->nIsRegistered = 0;
                                fseek($fpData, $prevPos, SEEK_SET);
                                WriteOneData($fpData, $oneData);
                                $bUnregistered = TRUE;
                            } else {        // "REGISTER"
                                $errMsg = "ConnectKey already occupied for server";
                            }
                        } else {
                            // for client; found
                            if ($oneData->nUdpClientPort !== 0)
                            {
                                $errMsg = "Client already registered";
                            }
                            else
                            {
                                $oneData->nUdpClientPort = $udpPort;
                                $oneData->nIpClientAsInt = StrIpToInt($ipPubl);
                                fseek($fpData, $prevPos, SEEK_SET);
                                WriteOneData($fpData, $oneData);

                                // refill output $ipData
                                $ipData = $oneData->ToIpData();
                            }
                        }

                        break;
                    }
                }
                $prevPos = ftell($fpData);
            }
        }

        if ("" === $errMsg && $isSvr0Client1 === 0 && $cmd === "REGISTER") {
            $ipSvrPubl = $ipPubl;
            $ipData =  $ipSvrPubl . "," . sprintf("%d", $udpPort) . "," . $localIP . "," .
                    "0.0.0.0" . "," . "0";     // client public ip and udpport (not yet registered)
            $oneData = new FfBinConnData();
            $oneData->nIsRegistered = 1;
            $oneData->strConnKey = $connectKey;
            $oneData->SetThingsFromIpData($ipData);
            WriteOneData($fpData, $oneData);
        }

        if (! is_null($fpData)) {
            fclose($fpData);
        }



        // JWdP 20250406 UNREGISTER is something that for the user is of less importance, so I use this
        // occasion to do some cleanup, if it's worthwhile. I now think of 'worthwhile' as 1000 or more
        // unregistered data found (but note: we have only counted the ones until we found the matching
        // FfBinConnData

        if ("" === $errMsg && $bUnregistered && $nUnregisteredCounter > 1000) {
            $errMsg = CleanupDataFile($strDataFile, $strDataFileTempCleanup);
        }

        if ("" === $errMsg && $cmd !== "GETDATA" && $isSvr0Client1 !== 0 && "" === $ipData)
            $errMsg = "No server found with this ConnectKey";
    }

    catch (Exception $ex)
    {
        $errMsg = "Internal server error: " . $ex->getMessage();
    }

    // VERY ESSENTIAL:
    BlockDeblock(FALSE);

    return $errMsg;
}


function IsRegistrationStillValid($oneData)
{
    $dtNow = new DateTime();
    return $oneData->nIsRegistered && $oneData->dtThisCreated->diff($dtNow)->days < 1;
}




// returns "" if success or errmsg if not
function BlockDeblock($blockFlag)
{
    $nNumTriesBlock = 50;
    $strFullBlockfile = "./farfilesdata.lck";

    if ($blockFlag)
    {
        $nTryCounter = 0;
        $dthServerBlocked = NULL;
        while (file_exists($strFullBlockfile))
        {
            if ($nTryCounter >= $nNumTriesBlock)
            {
                return "Internal server error (BLOCKED)";
            }

            usleep(100);		// 100 msecs
            $nTryCounter++;
        }

        //JWdP 20250405: HERE IS AN UNATOMIC MOMENT BETWEEN file_exists AND @fopen
        //but in case it happens, user will get an internal server error, this happens maybe once in a thousand times
        $fpBlock = @fopen($strFullBlockfile, "w");
        if ($fpBlock === FALSE)
            return "Internal server error (BLOCKERR)";
        fclose($fpBlock);
    }
    else		// deblock
    {
        //JWdP 20250405: ELib_FileDelete also HAS AN UNATOMIC MOMENT
        //but in case it happens, user will get an internal server error, this happens maybe once in thousand times
        if (! ELib_FileDelete($strFullBlockfile))
            return "Internal server error (DEBLOCKERR)";
    }

    return "";
}


function CleanupDataFile($strDataFile, $strDataFileTempCleanup)
{
    $fpData = @fopen($strDataFile, "rb");
    $fpDataNew = @fopen($strDataFileTempCleanup, "wb");

    while (($oneData = ReadOneData($fpData)) !== FALSE) {
        if (IsRegistrationStillValid($oneData)) {
            WriteOneData($fpDataNew, $oneData);
        }
    }
    fclose($fpDataNew);
    fclose($fpData);
    ELib_FileDelete($strDataFile);
    if (@rename($strDataFileTempCleanup, $strDataFile) === FALSE)
        return "CleanupDataFile: could not rename '$strDataFileTempCleanup' to '$strDataFile'";
    return "";
}




function EchoDataFile($strDataFile)
{
    $i = 0;
    $fpData = @fopen($strDataFile, "rb");

    echo "echo of file '$strDataFile':<br>";
    while (($oneData = ReadOneData($fpData)) !== FALSE) {
        $i++;
        echo "object $i:<br>";
        $oneData->EchoThis();
        echo "<br>";
    }
    fclose($fpData);
}

function WriteDataFileAsTxt($strDataFile, $strDumpTxtFile)
{
    $i = 0;
    $fpData = @fopen($strDataFile, "rb");
    $fpTxt = @fopen($strDumpTxtFile, "w");
    while (($oneData = ReadOneData($fpData)) !== FALSE) {
        $i++;
        $oneData->DumpThis($fpTxt, $i);
    }
    fclose($fpData);
}









// returns FALSE if file still exists after delete, else TRUE
function ELib_FileDelete($strFullFile)
{
    @unlink($strFullFile);
    return (! file_exists($strFullFile));
}

//=================================================================
// 07-Feb-2016 JWdP
// 12-Jun-2018 JWdP
// 		difference with H5Lib_BinReadXxxx: returns FALSE if there is no data; usage of feof is wrong
//		(this does not count in CadH5, because there we read always from position, and failure is
//		error)
//=================================================================
function ELib_FileBinRead($reader, $numBytes)
{
    $data = fread($reader, $numBytes);	// may be ""
    return ($data == "" && $numBytes > 0 ? FALSE : $data);
}

//=================================================================
// 06-Feb-2016 JWdP
// Copied from wwwDgh5PhpOLD pdgh5_FileBinReadInt
// 12-Jun-2018 JWdP
// 		difference with H5Lib_BinReadXxxx: returns FALSE if there is no data; usage of feof is wrong
//		(this does not count in CadH5, because there we read always from position, and failure is
//		error)
//=================================================================
function ELib_FileBinReadInt($reader)
{
    $data = ELib_FileBinRead($reader, 4);
    if ($data === FALSE)
        return FALSE;

    //$nJWdP = unpack("i", $data);
    //var_dump($data);
    //echo "JWdP DUMP<br>";
    //var_dump(nJWdP);

    $arrayFromUnpack = @unpack("i", $data);

    if ($arrayFromUnpack === false)
        throw new Exception(
            "ERROR: ELib_FileBinReadInt; 'unpack'-error (probably: not 4 bytes input)");

    $readInt = $arrayFromUnpack[1];
    return $readInt;
}

//=================================================================
// 06-Feb-2016 JWdP
// Copied from wwwDgh5PhpOLD pdgh5_FileBinReadDouble
// 12-Jun-2018 JWdP
// 		difference with H5Lib_BinReadXxxx: returns FALSE if there is no data; usage of feof is wrong
//=================================================================
function ELib_FileBinReadDouble($reader)
{
    $data = ELib_FileBinRead($reader, 8);
    if ($data === FALSE)
        return FALSE;

    $arrayFromUnpack = unpack("d", $data);
    if ($arrayFromUnpack === false)
        throw new Exception(
            "ERROR: ELib_FileBinReadDouble; 'unpack'-error (probably: not 8 bytes input)");

    $readDouble = $arrayFromUnpack[1];
    return $readDouble;
}


//=================================================================
// 06-Feb-2016 JWdP
// Copied from  pdgh5_FileBinReadString
// 12-Jun-2018 JWdP
// 		difference with H5Lib_BinReadXxxx: returns FALSE if there is no data; usage of feof is wrong
//=================================================================
function ELib_FileBinReadString($reader, $nStringLength)
{
    if ($nStringLength <= 0)
        return "";
    $retStr = ELib_FileBinRead($reader, $nStringLength);
    if ($retStr === FALSE)
        return FALSE;

    $lenStr = strlen($retStr);
    if ($lenStr < $nStringLength)
        throw new Exception(
            "ERROR: ELib_FileBinReadString; error, expected " .
            $nStringLength . " byte(s), got " . lenStr);
    return $retStr;
}

//JWdP 20180609
function ELib_FileBinReadStrlenThenString($reader)
{
    $nStrLen = ELib_FileBinReadInt($reader);
    if ($nStrLen === FALSE)
        return FALSE;

    return ELib_FileBinReadString($reader, $nStrLen);
}




//JWdP 20180609
function ELib_FileBinWrite($writer, $data, $numBytes)
{
    fwrite($writer, $data, $numBytes);
}

//JWdP 20180609
function ELib_FileBinWriteInt($writer, $nInt)
{
    fwrite($writer, @pack("i", $nInt), 4);
}

//JWdP 20180609
function ELib_FileBinWriteDouble($writer, $dDbl)
{
    fwrite($writer, @pack("d", $dDbl), 8);
}

//JWdP 20180609
function ELib_FileBinWriteStrlenThenString($writer, $str)
{
    $nStrLen = strlen($str);
    ELib_FileBinWriteInt($writer, $nStrLen);
    ELib_FileBinWrite($writer, $str, $nStrLen);
}



//JWdP 20180708
function ELib_FileBinWriteInt_Encr($writer, $nInt)
{
    $bindata = @pack("i", $nInt);	// -> a 'binary string', which actually is just a string
    ELib_EncrBinData($bindata);
    fwrite($writer, $bindata, 4);
}

function ELib_FileBinReadInt_Decr($reader)
{
    $data = ELib_FileBinRead($reader, 4);
    if ($data === FALSE)
        return FALSE;

    ELib_DecrBinData($data);
    $arrayFromUnpack = @unpack("i", $data);

    if ($arrayFromUnpack === false)
        throw new Exception(
            "ERROR: ELib_FileBinReadInt_Decr; 'unpack'-error (probably: not 4 bytes input)");

    return $arrayFromUnpack[1];			// yes, [1] ! not [0]
}

function ELib_FileBinWriteStrlenThenString_Encr($writer, $str)
{
    //JWdP 20180708 Tremendous problems because PHP sees string "1" sometimes as int 1
    //This turned out now that I have began to encrypt (ord($str[$i] did not work out))
    $strReallyStr = "" . $str;
    ELib_EncrBinData($strReallyStr);
    $nStrLen = strlen($strReallyStr);
    ELib_FileBinWriteInt_Encr($writer, $nStrLen);
    ELib_FileBinWrite($writer, $strReallyStr, $nStrLen);
}


function ELib_FileBinReadStrlenThenString_Decr($reader)
{
    $nStrLen = ELib_FileBinReadInt_Decr($reader);
    if ($nStrLen === FALSE)
        return FALSE;

    return ELib_FileBinReadString_Decr($reader, $nStrLen);
}

function ELib_FileBinReadString_Decr($reader, $nStringLength)
{
    if ($nStringLength <= 0)
        return "";
    $retStr = ELib_FileBinRead($reader, $nStringLength);
    if ($retStr === FALSE)
        return FALSE;

    ELib_DecrBinData($retStr);
    $lenStr = strlen($retStr);
    if ($lenStr < $nStringLength)
        throw new Exception(
            "ERROR: ELib_FileBinReadString_Decr; error, expected " .
            $nStringLength . " byte(s), got " . lenStr);
    return $retStr;
}


function ELib_EncrBinData(&$bindata)
{
    $nNum = strlen($bindata);
    for ($i=0; $i < $nNum; $i++)
    {
        $nX = ($i*19 + 123) % 256;
        $n = (ord($bindata[$i]) + $nX) % 256;
        $bindata[$i] = chr($n);
    }
}

function ELib_DecrBinData(&$bindata)
{
    // generated by ChatGPT
    $nNum = strlen($bindata);
    for ($i=0; $i < $nNum; $i++)
    {
        $nX = ($i*19 + 123) % 256;
        $n = (ord($bindata[$i]) - $nX) % 256;
        $bindata[$i] = chr($n);
    }
}


function GetStrIpFromInt($nInt)
{
    // generated by ChatGPT
    return sprintf(
        "%d.%d.%d.%d",
        ($nInt >> 24) & 0xFF,
        ($nInt >> 16) & 0xFF,
        ($nInt >> 8)  & 0xFF,
        $nInt & 0xFF
    );
}



function StrIpToInt($strIp)
{
    $parts = explode('.', $strIp);
    // Convert each octet to integer and compose the 32-bit value
    return ((int)$parts[0] << 24) |
        ((int)$parts[1] << 16) |
        ((int)$parts[2] << 8)  |
        (int)$parts[3];
}


// returns: string (bytes) object
function DateTimeToBytes(DateTime $dt)
{
    // Get number of microseconds since Unix epoch
    $timestamp = (int)($dt->format('Uu')); // 'U' = seconds, 'u' = microseconds

    // Pack it into 8 bytes (unsigned 64-bit)
    return pack('J', $timestamp);
}


// returns: DateTime object
function BytesToDateTime($bytes) {
    // Unpack the 8 bytes into an integer
    $arr = unpack('J', $bytes);
    $timestamp = $arr[1];

    // Separate seconds and microseconds
    $seconds = intdiv($timestamp, 1000000);
    $microseconds = $timestamp % 1000000;

    // Create DateTime from seconds
    $dt = (new DateTime())->setTimestamp($seconds);

    // Add microseconds
    $dt = $dt->modify("+{$microseconds} microseconds");

    return $dt;
}



// $oneData: must be FfBinConnData instance
function WriteOneData($fpData, FfBinConnData $oneData)
{
    ELib_FileBinWriteInt($fpData, $oneData->nIsRegistered);
    ELib_FileBinWriteStrlenThenString_Encr($fpData, $oneData->strConnKey);
    ELib_FileBinWriteInt_Encr($fpData, $oneData->nIpSvrAsInt);
    ELib_FileBinWriteInt_Encr($fpData, $oneData->nUdpSvrPort);
    ELib_FileBinWriteInt_Encr($fpData, $oneData->nLocalIpSvrAsInt);
    ELib_FileBinWriteInt_Encr($fpData, $oneData->nIpClientAsInt);
    ELib_FileBinWriteInt_Encr($fpData, $oneData->nUdpClientPort);
    ELib_FileBinWrite($fpData, DateTimeToBytes($oneData->dtThisCreated), 8);
}


// Returns: FfBinConnData class or FALSE if eof (or weird error)
function ReadOneData($fpData)
{
    $nIsRegistered = ELib_FileBinReadInt($fpData);
    if (FALSE === $nIsRegistered)
        return FALSE;

    $retData = new FfBinConnData();
    $retData->nIsRegistered = $nIsRegistered;
    $retData->strConnKey = ELib_FileBinReadStrlenThenString_Decr($fpData);
    $retData->nIpSvrAsInt = ELib_FileBinReadInt_Decr($fpData);
    $retData->nUdpSvrPort = ELib_FileBinReadInt_Decr($fpData);
    $retData->nLocalIpSvrAsInt = ELib_FileBinReadInt_Decr($fpData);
    $retData->nIpClientAsInt = ELib_FileBinReadInt_Decr($fpData);
    $retData->nUdpClientPort = ELib_FileBinReadInt_Decr($fpData);
    $retData->dtThisCreated = BytesToDateTime(ELib_FileBinRead($fpData, 8));
    return $retData;
}


class FfBinConnData
{
    public $nIsRegistered = 0;
    public $strConnKey = "";
    public $nIpSvrAsInt;
    public $nUdpSvrPort = 0;
    public $nLocalIpSvrAsInt = 0;
    public $nIpClientAsInt;
    public $nUdpClientPort = 0;
    public $dtThisCreated;

    public function __construct() {
        $this->dtThisCreated = new DateTime();
    }

    public function ToIpData()
    {
        return GetStrIpFromInt($this->nIpSvrAsInt) . "," .
            sprintf("%d", $this->nUdpSvrPort) . "," .
            GetStrIpFromInt($this->nLocalIpSvrAsInt) . "," .
            GetStrIpFromInt($this->nIpClientAsInt) . "," .
            sprintf("%d", $this->nUdpClientPort);
    }

    public function SetThingsFromIpData($ipData)
    {
        $arrParts = explode(",", $ipData);
        $this->nIpSvrAsInt = StrIpToInt($arrParts[0]);
        $this->nUdpSvrPort = intval($arrParts[1]);
        $this->nLocalIpSvrAsInt = StrIpToInt($arrParts[2]);
        $this->nIpClientAsInt = StrIpToInt($arrParts[3]);
        $this->nUdpClientPort = intval($arrParts[4]);
    }

    public function EchoThis()
    {
        echo "nIsRegistered=$this->nIsRegistered<br>";
        echo "strConnKey=$this->strConnKey<br>";
        echo "nIpSvrAsInt=$this->nIpSvrAsInt<br>";
        echo "nUdpSvrPort=$this->nUdpSvrPort<br>";
        echo "nLocalIpSvrAsInt=$this->nLocalIpSvrAsInt<br>";
        echo "nIpClientAsInt=$this->nIpClientAsInt<br>";
        echo "nUdpClientPort=$this->nUdpClientPort<br>";
        $diffWithNow = $this->dtThisCreated->diff(new DateTime());
        echo "dtThisCreated={$this->dtThisCreated->format('Y-m-d H:i:s.u')}" .
            ", days: {$diffWithNow->days}, minutes: {$diffWithNow->i}<br>";
    }


    public function DumpThis($fpTxt, $i = 0)
    {
        $diffWithNow = $this->dtThisCreated->diff(new DateTime());
        if (0 !== $i)
            fprintf($fpTxt, "%d ", $i);
        fprintf($fpTxt, "%d ", $this->nIsRegistered);
        fprintf($fpTxt, GetStrIpFromInt($this->nIpSvrAsInt) . " ");
        fprintf($fpTxt, "%7d ", $this->nUdpSvrPort);
        fprintf($fpTxt, GetStrIpFromInt($this->nLocalIpSvrAsInt) . " ");
        fprintf($fpTxt, GetStrIpFromInt($this->nIpClientAsInt) . " ");
        fprintf($fpTxt, "%7d ", $this->nUdpClientPort);
        fprintf($fpTxt, $this->dtThisCreated->format('Y-m-d H:i:s.u') . " ");
        fprintf($fpTxt, "%4d ", $diffWithNow->days);
        fprintf($fpTxt, $this->strConnKey);
        fprintf($fpTxt, "\n");
    }
}

?>

