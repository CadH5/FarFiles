using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public enum MsgSvrClType
    {
        UNMATCHED_SIGNORTYPE,
        ERROR,
        DISCONN_INFO,
        STRING_SEND,
        STRING_ANS,
        PATHINFO_REQ,
        PATHINFONEXT_REQ,
        PATHINFO_ANS,
        PATHINFO_ANDROIDBUSY,
        PATHINFO_ISANDRBUSYINQ,
        COPY_REQ,
        COPYNEXT_REQ,
        COPY_ANS,
        ABORTED_INFO,
        ABORTED_CONFIRM,
        COPY_TOSVRPART,
        COPY_TOSVRCONFIRM,
        SWAP_REQ,
        SWAPREQ_CONFIRM,
        SWAPREQ_REJECTED,
    }

    public class MsgSvrClBase
    {
        /// <summary>
        /// FARFILESMSG_SIGN_AND_VERSION: 12344321 is v1, 12344322 is v2, etc, indicating msg protocol version
        /// </summary>
        public const int FARFILESMSG_SIGN_AND_VERSION = 12344321;

        /// <summary>
        /// FARFILESMSG_MINSIGN: signature of first msg protocol version published
        /// </summary>
        public const int FARFILESMSG_MINSIGN = 12344321;

        public const int MAXNUMARR_TOAVOIDMEMERR = 1000000;
        public byte[] Bytes;

        /// <summary>
        /// MsgsProtocolVersion: starts with 1; is incremented when protocol changes in new published FarFiles version
        /// </summary>
        public int MsgsProtocolVersion { get; protected set; } = 0;

        // after signature and type:
        protected const int STARTINDEX_INDIVIDUAL = sizeof(int) + sizeof(int);

        public MsgSvrClType Type
        {
            get
            {
                return TypeFromBytes(Bytes, out int sign);
            }
        }


        /// <summary>
        /// Base ctor that only sets MsgSvrClType in new Bytes array
        /// </summary>
        /// <param name="type"></param>
        protected MsgSvrClBase(MsgSvrClType type)
        {
            var bytes = BitConverter.GetBytes(FARFILESMSG_SIGN_AND_VERSION).ToList();
            bytes.AddRange(BitConverter.GetBytes((int)type));
            Bytes = bytes.ToArray();
        }


        /// <summary>
        /// Base ctor that copies bytes to member array Bytes, and checks expectedType
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="expectedType"></param>
        /// <exception cref="Exception"></exception>
        protected MsgSvrClBase(byte[] bytes, MsgSvrClType expectedType)
        {
            MsgSvrClType foundType = TypeFromBytes(bytes, out int sign);
            if (foundType != expectedType)
            {
                if (foundType == MsgSvrClType.ERROR)
                    throw new Exception(new MsgSvrClErrorAnswer(bytes).GetErrMsgsJoined());

                string startsWithDescr = "";
                if (foundType == MsgSvrClType.UNMATCHED_SIGNORTYPE)
                    startsWithDescr =
                        $" (bytes start with '{MauiProgram.DispStartBytes(bytes, 0, 30)}')'";

                throw new Exception($"Expected message type: {expectedType}, got: {foundType}{startsWithDescr}");
            }
            Bytes = bytes;      // reference, no copy!
            MsgsProtocolVersion = sign - FARFILESMSG_MINSIGN + 1;
        }


        /// <summary>
        /// Returns null if type is UNMATCHED_SIGNORTYPE (message should be ignored), else a MsgSvrClBase instance
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static MsgSvrClBase CreateFromBytes(byte[] bytes)
        {
            MsgSvrClType type = TypeFromBytes(bytes, out int sign);
            switch (type)
            {
                case MsgSvrClType.UNMATCHED_SIGNORTYPE:
                    return null;

                case MsgSvrClType.ERROR:
                    return new MsgSvrClErrorAnswer(bytes);

                case MsgSvrClType.DISCONN_INFO:
                    return new MsgSvrClDisconnInfo(bytes);

                case MsgSvrClType.STRING_SEND:
                    return new MsgSvrClStringSend(bytes);

                case MsgSvrClType.STRING_ANS:
                    return new MsgSvrClStringAnswer(bytes);

                case MsgSvrClType.PATHINFO_REQ:
                    return new MsgSvrClPathInfoRequest(bytes);

                case MsgSvrClType.PATHINFONEXT_REQ:
                    return new MsgSvrClPathInfoNextpartRequest(bytes);

                case MsgSvrClType.PATHINFO_ANS:
                    return new MsgSvrClPathInfoAnswer(bytes);

                case MsgSvrClType.PATHINFO_ANDROIDBUSY:
                    return new MsgSvrClPathInfoAndroidBusy(bytes);

                case MsgSvrClType.PATHINFO_ISANDRBUSYINQ:
                    return new MsgSvrClPathInfoAndroidStillBusyInq(bytes);

                case MsgSvrClType.COPY_REQ:
                    return new MsgSvrClCopyRequest(bytes);

                case MsgSvrClType.COPYNEXT_REQ:
                    return new MsgSvrClCopyNextpartRequest(bytes);

                case MsgSvrClType.COPY_ANS:
                    return new MsgSvrClCopyAnswer(bytes);

                case MsgSvrClType.ABORTED_INFO:
                    return new MsgSvrClAbortedInfo(bytes);

                case MsgSvrClType.ABORTED_CONFIRM:
                    return new MsgSvrClAbortedConfirmation(bytes);

                case MsgSvrClType.COPY_TOSVRPART:
                    return new MsgSvrClCopyToSvrPart(bytes);

                case MsgSvrClType.COPY_TOSVRCONFIRM:
                    return new MsgSvrClCopyToSvrConfirmation(bytes);

                case MsgSvrClType.SWAP_REQ:
                    return new MsgSvrClSwapRequest(bytes);

                case MsgSvrClType.SWAPREQ_CONFIRM:
                    return new MsgSvrClSwapReqReceivedConfirm(bytes);

                case MsgSvrClType.SWAPREQ_REJECTED:
                    return new MsgSvrClSwapRejectedBySvr(bytes);

                default:
                    throw new Exception(
                        $"PROGRAMMERS: CreateFromBytes(): not handled type {type}");
            }
        }

        /// <summary>
        /// Throws exception if this is not expectedType;
        /// if this is MsgSvrClErrorAnswer then exceptiontext is the errormessage
        /// </summary>
        /// <param name="expectedType"></param>
        /// <exception cref="Exception"></exception>
        public void CheckExpectedTypeMaybeThrow(Type expectedType)
        {
            if (this.GetType() != expectedType)
            {
                if (this is MsgSvrClErrorAnswer)
                    throw new Exception(((MsgSvrClErrorAnswer)this).GetErrMsgsJoined());

                throw new Exception(
                    $"Expected message: {expectedType}, got: {this.GetType()}");
            }
        }


        protected void AddBytes(IEnumerable<byte> bytes)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(bytes);
            Bytes = lisBytes.ToArray();
        }


        /// <summary>
        /// Tries to get int from first 4 bytes after the 4 signature bytes, and cast to MsgSvrClType
        /// Also checks signature (must be int from first to current msg version)
        /// If exception, returns UNMATCHED_SIGNORTYPE and message should be ignored (can happen as the updconnection is open)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="sign">first int found, is also checked</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected static MsgSvrClType TypeFromBytes(byte[] bytes, out int sign)
        {
            int iType = -1;
            sign = -1;
            try
            {
                sign = BitConverter.ToInt32(bytes, 0);
                if (sign < FARFILESMSG_MINSIGN || sign > FARFILESMSG_SIGN_AND_VERSION)
                    return MsgSvrClType.UNMATCHED_SIGNORTYPE;
                iType = BitConverter.ToInt32(bytes, sizeof(int));   // type comes after the (int) signature_and_verion
                return (MsgSvrClType)iType;
            }
            catch
            {
                return MsgSvrClType.UNMATCHED_SIGNORTYPE;
            }
        }




        /// <summary>
        /// Tries to copy MsgSvrClType into second 4 bytes (after signature); bytes must have already length >= 8
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static void CopyTypeIntoBytes(MsgSvrClType type, byte[] bytes)
        {
            int startIndexDest = sizeof(int);   // after signature
            Array.Copy(BitConverter.GetBytes((int)type),
                            0, bytes, startIndexDest, sizeof(int));
        }




        /// <summary>
        /// Convert str to bytes: first 4 bytes: length; then indication 1 or 2;
        /// then 1-byte-char by 1-byte-char; But if there are chars > 255: char by char
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] StrPlusLenToBytes(string str)
        {
            var retArr = new byte[sizeof(int) + 1 + str.Length];
            Array.Copy(BitConverter.GetBytes(str.Length), 0, retArr, 0, sizeof(int));
            int idxIndicationCharSize = sizeof(int);
            byte indicationCharSize = 1;         // 1 byte per char
            retArr[idxIndicationCharSize] = indicationCharSize;
            int i = idxIndicationCharSize + 1;
            foreach (char c in str)
            {
                uint uC = (uint)c;
                if (uC > 255)
                {
                    indicationCharSize = 2;
                    break;
                }
                retArr[i++] = (byte)uC;
            }

            if (indicationCharSize == 1)
                return retArr;

            // indicationCharSize is 2: there are UTF-16 chars in the string.
            retArr = new byte[sizeof(int) + 1 + 2*str.Length];
            Array.Copy(BitConverter.GetBytes(str.Length), 0, retArr, 0, sizeof(int));
            retArr[idxIndicationCharSize] = indicationCharSize;     // is 2 here
            i = idxIndicationCharSize + 1;
            foreach (char c in str)
            {
                Array.Copy(BitConverter.GetBytes(c), 0, retArr, i, sizeof(char));
                i += sizeof(char);
            }

            return retArr;
        }


        public static string StrPlusLenFromBytes(byte[] bytes, ref int index)
        {
            int lenHdr = sizeof(int) + 1;
            if (bytes.Length - index < lenHdr)
                throw new Exception(
                    $"PROGRAMMERS: StrPlusLenFromBytes: Length ({bytes.Length}) less than {lenHdr} bytes");

            int strLen = BitConverter.ToInt32(bytes, index);
            index += sizeof(int);
            byte indicationCharSize = bytes[index++];

            int till = index + strLen * indicationCharSize;     // supposing sizeof(char) == 2 forever
            if (bytes.Length < till)
                throw new Exception(
                    $"PROGRAMMERS: StrPlusLenFromBytes: Length ({bytes.Length}) less than {till} bytes");

            string retStr = "";
            if (indicationCharSize == 1)
            {
                for (; index < till; index++)
                {
                    retStr += (char)bytes[index];
                }
            }
            else if (indicationCharSize == 2)
            {
                for (; index < till; index += sizeof(char))
                {
                    retStr += BitConverter.ToChar(bytes, index);
                }
            }

            return retStr;
        }

        public static void AddNumAndStringsToLisBytes(List<byte> lisBytes,
                    IEnumerable<string> strings)
        {
            lisBytes.AddRange(BitConverter.GetBytes(strings.Count()));
            foreach (string str in strings)
            {
                lisBytes.AddRange(StrPlusLenToBytes(str));
            }
        }

        /// <summary>
        /// Returns string[] from Bytes member
        /// </summary>
        /// <param name="idx">after this, is at start next part of Bytes</param>
        /// <returns></returns>
        public string[] GetStringsAtIndex(ref int idx)
        {
            int numStrs = BitConverter.ToInt32(Bytes, idx);
            idx += sizeof(int);
            if (numStrs < 0 || numStrs > MAXNUMARR_TOAVOIDMEMERR)
                throw new Exception("GetStringsAtIndex: " +
                    $"invalid in bytes: found num strings {numStrs}");

            var retStrs = new string[numStrs];
            for (int i = 0; i < numStrs; i++)
            {
                retStrs[i] = StrPlusLenFromBytes(Bytes, ref idx);
            }
            return retStrs;
        }


        /// <summary>
        /// Returns fileNames and fileSizes from bytes
        /// </summary>
        /// <param name="idx">after this, is at start next part of Bytes</param>
        /// <returns></returns>
        public string[] GetFileNamesAndSizesAndDtsAtIndex(ref int idx,
                    out long[] fileSizes, out DateTime[] dtLastWrites)
        {
            int num = BitConverter.ToInt32(Bytes, idx);
            idx += sizeof(int);
            if (num < 0 || num > MAXNUMARR_TOAVOIDMEMERR)
                throw new Exception($"GetFileNamesAndSizesAtIndex: " +
                    $"invalid in bytes: found num {num} for arrays");

            fileSizes = new long[num];
            dtLastWrites = new DateTime[num];
            var fileNames = new string[num];

            for (int i = 0; i < num; i++)
            {
                fileSizes[i] = BitConverter.ToInt64(Bytes, idx);
                idx += sizeof(long);
                dtLastWrites[i] = DateTime.FromBinary(BitConverter.ToInt64(Bytes, idx));
                idx += sizeof(long);
                fileNames[i] = StrPlusLenFromBytes(Bytes, ref idx);
            }
            return fileNames;
        }



        public static void CopyBytesToList(List<byte> lisBytes, int startIdxInLis, byte[] from)
        {
            int idx = startIdxInLis;
            foreach (byte b in from)
            {
                lisBytes[idx++] = b;
            }
        }
    }



    /// <summary>
    /// MsgSvrClErroranswer
    /// </summary>
    public class MsgSvrClErrorAnswer : MsgSvrClBase
    {
        public MsgSvrClErrorAnswer(string[] errMsgs)
            : base(MsgSvrClType.ERROR)
        {
            var lisBytes = Bytes.ToList();
            AddNumAndStringsToLisBytes(lisBytes, errMsgs);
            Bytes = lisBytes.ToArray();
        }

        public MsgSvrClErrorAnswer(string errMsg)
            : this(new string[] {errMsg})
        {
        }


        public MsgSvrClErrorAnswer(byte[] bytes)
            : base(bytes, MsgSvrClType.ERROR)
        {
        }


        public string[] GetErrMsgs()
        {
            int idx = STARTINDEX_INDIVIDUAL;
            return GetStringsAtIndex(ref idx);
        }

        /// <summary>
        /// Get all errmsgs joined by System.Environment.NewLine
        /// </summary>
        /// <returns></returns>
        public string GetErrMsgsJoined()
        {
            return String.Join(System.Environment.NewLine, GetErrMsgs());
        }
    }







    /// <summary>
    /// Info that other end disconnected
    /// </summary>
    public class MsgSvrClDisconnInfo : MsgSvrClBase
    {
        public MsgSvrClDisconnInfo()
            : base(MsgSvrClType.DISCONN_INFO)
        {
        }


        public MsgSvrClDisconnInfo(byte[] bytes)
            : base(bytes, MsgSvrClType.DISCONN_INFO)
        {
        }
    }






    /// <summary>
    /// MsgSvrClStringSend
    /// </summary>
    public class MsgSvrClStringSend : MsgSvrClBase
    {
        public MsgSvrClStringSend(string strToSend)
            : base(MsgSvrClType.STRING_SEND)
        {
            AddBytes(StrPlusLenToBytes(strToSend));
        }

        public MsgSvrClStringSend(byte[] bytes)
            : base(bytes, MsgSvrClType.STRING_SEND)
        {
        }


        public string GetString()
        {
            int index = STARTINDEX_INDIVIDUAL;
            return StrPlusLenFromBytes((byte[])Bytes, ref index);
        }
    }



    /// <summary>
    /// MsgSvrClStringAnswer
    /// </summary>
    public class MsgSvrClStringAnswer : MsgSvrClBase
    {
        public MsgSvrClStringAnswer() : base(MsgSvrClType.STRING_ANS)
        {
            // just a confirmation
        }

        public MsgSvrClStringAnswer(byte[] bytes)
            : base(bytes, MsgSvrClType.STRING_ANS)
        {
        }
    }



    /// <summary>
    /// MsgSvrClPathInfoRequest
    /// Always first message by which client tries to connect to server.
    /// Server must refuse if it is already connected
    /// </summary>
    public class MsgSvrClPathInfoRequest : MsgSvrClBase
    {
        public MsgSvrClPathInfoRequest(Guid connClientGuid, IEnumerable<string> svrSubpathParts) : base(MsgSvrClType.PATHINFO_REQ)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(connClientGuid.ToByteArray());
            AddNumAndStringsToLisBytes(lisBytes, svrSubpathParts);
            Bytes = lisBytes.ToArray();
        }

        public MsgSvrClPathInfoRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.PATHINFO_REQ)
        {
        }

        public string[] GetConnclientguidAndSvrSubParts(out Guid connClientGuid)
        {
            try
            {
                int idx = STARTINDEX_INDIVIDUAL;
                var guidBytes = new byte[16];   // sizeof(Guid) seems impossible
                Array.Copy(Bytes, idx, guidBytes, 0, 16);
                idx += 16;
                connClientGuid = new Guid(guidBytes);
                return GetStringsAtIndex(ref idx);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error interpreting connClientGuid or serverPathParts from message: {exc.Message}");
            }
        }
    }




    /// <summary>
    /// Request next part of path info
    /// </summary>
    public class MsgSvrClPathInfoNextpartRequest : MsgSvrClBase
    {
        public MsgSvrClPathInfoNextpartRequest()
            : base(MsgSvrClType.PATHINFONEXT_REQ)
        {
        }


        public MsgSvrClPathInfoNextpartRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.PATHINFONEXT_REQ)
        {
        }
    }





    /// <summary>
    /// MsgSvrClPathInfoAnswer
    /// </summary>
    public class MsgSvrClPathInfoAnswer : MsgSvrClBase
    {
        public int SeqNr
        {
            get =>
                BitConverter.ToInt32(Bytes, STARTINDEX_INDIVIDUAL);
        }

        public MsgSvrClPathInfoAnswer(int seqNr, bool svrIsWritable, PathInfoAnswerState pathInfoAnswerState,
                    int bufSizeMoreOrLess)
            : base(MsgSvrClType.PATHINFO_ANS)
        {
            int numFolders = 0;
            int numFiles = 0;

            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(seqNr));
            lisBytes.AddRange(BitConverter.GetBytes(svrIsWritable));

            int idxNumFiles = -1;
            int idxNumFolders = lisBytes.Count;
            lisBytes.AddRange(BitConverter.GetBytes(0));    // temp number 0 will be replaced

            bool prevIsDir = true;
            while (pathInfoAnswerState.GetNextFileOrFolder(out bool isDir,
                            out string name, out long fileSize, out DateTime dtLastWrite))
            {
                if (prevIsDir && !isDir)
                {
                    // Start of files section
                    idxNumFiles = lisBytes.Count;
                    lisBytes.AddRange(BitConverter.GetBytes(0));    // temp number 0 will be replaced
                }

                if (!isDir)
                {
                    numFiles++;
                    lisBytes.AddRange(BitConverter.GetBytes(fileSize));
                    lisBytes.AddRange(BitConverter.GetBytes(dtLastWrite.ToBinary()));
                }
                else
                {
                    numFolders++;
                }
                lisBytes.AddRange(StrPlusLenToBytes(name));

                prevIsDir = isDir;
                if (lisBytes.Count >= bufSizeMoreOrLess)
                    break;
            }

            if (-1 == idxNumFiles)      // only folders
            {
                idxNumFiles = lisBytes.Count;
                lisBytes.AddRange(BitConverter.GetBytes(0));    // temp number 0 will be replaced
            }

            // overwrite temp numbers:
            CopyBytesToList(lisBytes, idxNumFolders, BitConverter.GetBytes(numFolders));
            CopyBytesToList(lisBytes, idxNumFiles, BitConverter.GetBytes(numFiles));

            // isLast:
            lisBytes.AddRange(BitConverter.GetBytes(pathInfoAnswerState.EndReached));

            Bytes = lisBytes.ToArray();
        }


        public MsgSvrClPathInfoAnswer(byte[] bytes)
            : base(bytes, MsgSvrClType.PATHINFO_ANS)
        {
        }


        public void GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizesAndDts(
                    out int seqNr, out bool isSvrWritable, out bool isLast,
                    out string[] folderNames, out string[] fileNames, out long[] fileSizes,
                    out DateTime[] dtLastWrites)
        {
            try
            {
                int idx = STARTINDEX_INDIVIDUAL;

                seqNr = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                isSvrWritable = BitConverter.ToBoolean(Bytes, idx);
                idx += sizeof(bool);
                folderNames = GetStringsAtIndex(ref idx);
                fileNames = GetFileNamesAndSizesAndDtsAtIndex(ref idx, out fileSizes, out dtLastWrites);
                isLast = BitConverter.ToBoolean(Bytes, idx);
                idx += sizeof(bool);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error interpreting folder- and filenames and filesizes from message: {exc.Message}");
            }
        }
    }



    /// <summary>
    /// Indication from server to client that Android is busy collecting filedata
    /// and that client best goes sleeping a while
    /// </summary>
    public class MsgSvrClPathInfoAndroidBusy : MsgSvrClBase
    {
        public MsgSvrClPathInfoAndroidBusy(int seqNr)
            : base(MsgSvrClType.PATHINFO_ANDROIDBUSY)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(seqNr));
            Bytes = lisBytes.ToArray();
        }

        public MsgSvrClPathInfoAndroidBusy(byte[] bytes)
            : base(bytes, MsgSvrClType.PATHINFO_ANDROIDBUSY)
        {
        }

        public void GetSeqnr(out int seqNr)
        {
            try
            {
                int idx = STARTINDEX_INDIVIDUAL;
                seqNr = BitConverter.ToInt32(Bytes, idx);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error getting data from message: {exc.Message}");
            }
        }
    }




    /// <summary>
    /// Query from client to server whether Android is still busy
    /// </summary>
    public class MsgSvrClPathInfoAndroidStillBusyInq : MsgSvrClBase
    {
        public MsgSvrClPathInfoAndroidStillBusyInq()
            : base(MsgSvrClType.PATHINFO_ISANDRBUSYINQ)
        {
        }

        public MsgSvrClPathInfoAndroidStillBusyInq(byte[] bytes)
            : base(bytes, MsgSvrClType.PATHINFO_ISANDRBUSYINQ)
        {
        }
    }




    /// <summary>
    /// Request to copy files and folders
    /// </summary>
    public class MsgSvrClCopyRequest : MsgSvrClBase
    {
        public MsgSvrClCopyRequest(IEnumerable<string> svrSubpathParts,
                            IEnumerable<string> folderNames,
                            IEnumerable<string> fileNames)
            : base(MsgSvrClType.COPY_REQ)
        {
            var lisBytes = Bytes.ToList();
            AddNumAndStringsToLisBytes(lisBytes, svrSubpathParts);
            AddNumAndStringsToLisBytes(lisBytes, folderNames);
            AddNumAndStringsToLisBytes(lisBytes, fileNames);
            Bytes = lisBytes.ToArray();
        }


        public MsgSvrClCopyRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.COPY_REQ)
        {
        }


        public void GetSubPartsAndFolderAndFileNames(out string[] svrSubParts,
                out string[] folderNames, out string[] fileNames)
        {
            try
            {
                int idx = STARTINDEX_INDIVIDUAL;
                svrSubParts = GetStringsAtIndex(ref idx);
                folderNames = GetStringsAtIndex(ref idx);
                fileNames = GetStringsAtIndex(ref idx);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error interpreting strings from message: {exc.Message}");
            }
        }
    }



    /// <summary>
    /// Request next part of files and folders
    /// </summary>
    public class MsgSvrClCopyNextpartRequest : MsgSvrClBase
    {
        public MsgSvrClCopyNextpartRequest()
            : base(MsgSvrClType.COPYNEXT_REQ)
        {
        }


        public MsgSvrClCopyNextpartRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.COPYNEXT_REQ)
        {
        }
    }



    /// <summary>
    /// Answer with copydata, probably a part.
    /// (bufSizeMoreOrLess here is managed by CopyMgr)
    /// </summary>
    public class MsgSvrClCopyAnswer : MsgSvrClBase
    {
        public MsgSvrClCopyAnswer(int seqNr, bool isLastPart, byte[] data)
            : base(MsgSvrClType.COPY_ANS)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(seqNr));
            lisBytes.AddRange(BitConverter.GetBytes(isLastPart));
            lisBytes.AddRange(data);
            Bytes = lisBytes.ToArray();
        }


        public MsgSvrClCopyAnswer(byte[] bytes, MsgSvrClType? msgTypeOverride = null)
            : base(bytes, msgTypeOverride ?? MsgSvrClType.COPY_ANS)
        {
        }


        public int SeqNr
        {
            get =>
                BitConverter.ToInt32(Bytes, STARTINDEX_INDIVIDUAL);
        }
        public bool IsLastPart
        {
            get =>
                BitConverter.ToBoolean(Bytes, STARTINDEX_INDIVIDUAL + sizeof(int));
        }

        public void GetSeqnrAndIslastAndData(out int seqNr, out bool isLast, out byte[] data)
        {
            try
            {
                int idx = STARTINDEX_INDIVIDUAL;
                seqNr = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                isLast = BitConverter.ToBoolean(Bytes, idx);
                idx += sizeof(bool);
                int dataLen = Bytes.Length - idx;
                data = new byte[dataLen];
                Array.Copy(Bytes, idx, data, 0, dataLen);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error getting data from message: {exc.Message}");
            }
        }
    }





    /// <summary>
    /// Info from client that user aborted operation
    /// </summary>
    public class MsgSvrClAbortedInfo : MsgSvrClBase
    {
        public MsgSvrClAbortedInfo()
            : base(MsgSvrClType.ABORTED_INFO)
        {
        }


        public MsgSvrClAbortedInfo(byte[] bytes)
            : base(bytes, MsgSvrClType.ABORTED_INFO)
        {
        }
    }




    /// <summary>
    /// Info from client that user aborted operation
    /// </summary>
    public class MsgSvrClAbortedConfirmation : MsgSvrClBase
    {
        public MsgSvrClAbortedConfirmation()
            : base(MsgSvrClType.ABORTED_CONFIRM)
        {
        }


        public MsgSvrClAbortedConfirmation(byte[] bytes)
            : base(bytes, MsgSvrClType.ABORTED_CONFIRM)
        {
        }
    }





    /// <summary>
    /// This class has the very same functionality as MsgSvrClCopyAnswer,
    /// but is separated for sake of more clarity; used to copy FROM client TO server
    /// </summary>
    public class MsgSvrClCopyToSvrPart : MsgSvrClCopyAnswer
    {
        public MsgSvrClCopyToSvrPart(int seqNr, bool isLastPart, byte[] data)
            : base(seqNr, isLastPart, data)
        {
            // This has filled the wrong MsgSvrClType COPY_ANS
            CopyTypeIntoBytes(MsgSvrClType.COPY_TOSVRPART, Bytes);
        }

        public MsgSvrClCopyToSvrPart(byte[] bytes)
            : base(bytes, MsgSvrClType.COPY_TOSVRPART)
        {
            // This has filled the wrong MsgSvrClType COPY_ANS
            CopyTypeIntoBytes(MsgSvrClType.COPY_TOSVRPART, Bytes);
        }

    }




    /// <summary>
    /// This is the answer from server upon MsgSvrClCopyToSvrPart; just a confirmation
    /// </summary>
    public class MsgSvrClCopyToSvrConfirmation : MsgSvrClBase
    {
        public MsgSvrClCopyToSvrConfirmation(CopyCounters nums, int numErrMsgs,
            string firstErrMsg)
            : base(MsgSvrClType.COPY_TOSVRCONFIRM)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(nums.FoldersCreated));
            lisBytes.AddRange(BitConverter.GetBytes(nums.FilesCreated));
            lisBytes.AddRange(BitConverter.GetBytes(nums.FilesOverwritten));
            lisBytes.AddRange(BitConverter.GetBytes(nums.FilesSkipped));
            lisBytes.AddRange(BitConverter.GetBytes(nums.DtProblems));
            lisBytes.AddRange(BitConverter.GetBytes(nums.ErrHashesDiff));
            lisBytes.AddRange(BitConverter.GetBytes(numErrMsgs));
            lisBytes.AddRange(StrPlusLenToBytes(firstErrMsg));
            Bytes = lisBytes.ToArray();
        }


        public MsgSvrClCopyToSvrConfirmation(byte[] bytes)
            : base(bytes, MsgSvrClType.COPY_TOSVRCONFIRM)
        {
        }


        public void GetNumsAndFirstErrMsg(out CopyCounters nums,
                    out int numErrMsgs, out string firstErrMsg)
        {
            try
            {
                nums = new CopyCounters();

                int idx = STARTINDEX_INDIVIDUAL;
                nums.FoldersCreated = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                nums.FilesCreated = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                nums.FilesOverwritten = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                nums.FilesSkipped = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                nums.DtProblems = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                nums.ErrHashesDiff = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                numErrMsgs = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                firstErrMsg = StrPlusLenFromBytes(Bytes, ref idx);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error getting numbers from message: {exc.Message}");
            }
        }
    }




    /// <summary>
    /// request from client to swap client/server mode
    /// </summary>
    public class MsgSvrClSwapRequest : MsgSvrClBase
    {
        public MsgSvrClSwapRequest()
            : base(MsgSvrClType.SWAP_REQ)
        {
        }


        public MsgSvrClSwapRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.SWAP_REQ)
        {
        }
    }



    /// <summary>
    /// confirmation from server that swap requst was received (not yet agreed)
    /// </summary>
    public class MsgSvrClSwapReqReceivedConfirm : MsgSvrClBase
    {
        public MsgSvrClSwapReqReceivedConfirm()
            : base(MsgSvrClType.SWAPREQ_CONFIRM)
        {
        }


        public MsgSvrClSwapReqReceivedConfirm(byte[] bytes)
            : base(bytes, MsgSvrClType.SWAPREQ_CONFIRM)
        {
        }
    }



    /// <summary>
    /// message from server that user rejected swap request
    /// </summary>
    public class MsgSvrClSwapRejectedBySvr : MsgSvrClBase
    {
        public MsgSvrClSwapRejectedBySvr()
            : base(MsgSvrClType.SWAPREQ_REJECTED)
        {
        }


        public MsgSvrClSwapRejectedBySvr(byte[] bytes)
            : base(bytes, MsgSvrClType.SWAPREQ_REJECTED)
        {
        }
    }

}

