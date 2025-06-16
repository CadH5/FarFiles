//JEEWEE
//using CoreFoundation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//JEEWEE
//using static CoreFoundation.DispatchSource;

namespace FarFiles.Model
{
    public enum MsgSvrClType
    {
        ERROR,
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
    }

    public class MsgSvrClBase
    {
        public byte[] Bytes;

        public MsgSvrClType Type
        {
            get
            {
                try
                {
                    return (MsgSvrClType)BitConverter.ToInt32(Bytes);
                }
                catch
                {
                    return MsgSvrClType.ERROR;
                }
            }
        }


        /// <summary>
        /// Base ctor that only sets MsgSvrClType in new Bytes array
        /// </summary>
        /// <param name="type"></param>
        protected MsgSvrClBase(MsgSvrClType type)
        {
            Bytes = BitConverter.GetBytes((int)type);
        }


        /// <summary>
        /// Base ctor that copies bytes to member array Bytes, and checks expectedType
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="expectedType"></param>
        /// <exception cref="Exception"></exception>
        protected MsgSvrClBase(byte[] bytes, MsgSvrClType expectedType)
        {
            MsgSvrClType foundType = TypeFromBytes(bytes);
            if (foundType != expectedType)
            {
                if (foundType == MsgSvrClType.ERROR)
                    throw new Exception(new MsgSvrClErrorAnswer(bytes).GetErrMsg());

                throw new Exception($"Expected message type: {expectedType}, got: {foundType}");
            }
            Bytes = bytes;      // reference, no copy!
        }


        public static MsgSvrClBase CreateFromBytes(byte[] bytes)
        {
            MsgSvrClType type = TypeFromBytes(bytes);
            switch (type)
            {
                case MsgSvrClType.ERROR:
                    return new MsgSvrClErrorAnswer(bytes);

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
                    throw new Exception(((MsgSvrClErrorAnswer)this).GetErrMsg());

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
        /// Tries to get int from first 4 bytes, and cast to MsgSvrClType
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static MsgSvrClType TypeFromBytes(byte[] bytes)
        {
            int iType = -1;
            try
            {
                iType = BitConverter.ToInt32(bytes, 0);
                return (MsgSvrClType)iType;
            }
            catch (Exception exc)
            {
                throw new Exception($"Received invalid FarFiles message" +
                    (iType == -1 ? "" : $" starting with int {iType}") +
                    ": " + exc.Message);
            }
        }




        /// <summary>
        /// Tries to copy MsgSvrClType into first 4 bytes; bytes must have already length >= 4
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static void CopyTypeIntoBytes(MsgSvrClType type, byte[] bytes)
        {
            Array.Copy(BitConverter.GetBytes((int)type),
                            0, bytes, 0, sizeof(int));
        }




        /// <summary>
        /// Convert str to bytes: first 4 bytes: length; then char by char
        /// (so does not do unicode)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] StrPlusLenToBytes(string str)
        {
            var retArr = new byte[4 + str.Length];
            Array.Copy(BitConverter.GetBytes(str.Length), 0, retArr, 0, 4);
            int i = 4;
            foreach (char c in str)
            {
                uint uC = (uint)c;
                retArr[i++] = uC > 255 ? (byte)'.' : (byte)uC;
            }

            return retArr;
        }


        public static string StrPlusLenFromBytes(byte[] bytes, ref int index)
        {
            string ret = StrPlusLenFromBytes(bytes, index);
            index += 4 + ret.Length;
            return ret;
        }

        public static string StrPlusLenFromBytes(byte[] bytes, int indexStart = 0)
        {
            if (bytes.Length - indexStart < 4)
                throw new Exception(
                    $"PROGRAMMERS: StrPlusLenFromBytes: Length ({bytes.Length}) less than 4 bytes");

            int strLen = BitConverter.ToInt32(bytes, indexStart);
            int till = indexStart + 4 + strLen;
            if (bytes.Length < till)
                throw new Exception(
                    $"PROGRAMMERS: StrPlusLenFromBytes: Length ({bytes.Length}) less than {till} bytes");

            string retStr = "";
            for (int i = 4 + indexStart; i < till; i++)
            {
                retStr += (char)bytes[i];
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
            var retStrs = new string[numStrs];
            idx += sizeof(int);
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
        public string[] GetFileNamesAndSizesAtIndex(ref int idx, out long[] fileSizes)
        {
            int num = BitConverter.ToInt32(Bytes, idx);
            idx += sizeof(int);

            fileSizes = new long[num];
            var fileNames = new string[num];

            for (int i = 0; i < num; i++)
            {
                fileSizes[i] = BitConverter.ToInt64(Bytes, idx);
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
        public MsgSvrClErrorAnswer(string errMsg)
            : base(MsgSvrClType.ERROR)
        {
            AddBytes(StrPlusLenToBytes(errMsg));
        }

        public MsgSvrClErrorAnswer(byte[] bytes)
            : base(bytes, MsgSvrClType.ERROR)
        {
        }


        public string GetErrMsg()
        {
            return StrPlusLenFromBytes((byte[])Bytes, 4);
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
            return StrPlusLenFromBytes((byte[])Bytes, 4);
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
    /// JEEWEE! Client must send msg to server when it disconnects.
    /// Server must refuse if it is already connected
    /// </summary>
    public class MsgSvrClPathInfoRequest : MsgSvrClBase
    {
        public MsgSvrClPathInfoRequest(IEnumerable<string> svrSubpathParts) : base(MsgSvrClType.PATHINFO_REQ)
        {
            var lisBytes = Bytes.ToList();
            AddNumAndStringsToLisBytes(lisBytes, svrSubpathParts);
            Bytes = lisBytes.ToArray();
        }

        public MsgSvrClPathInfoRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.PATHINFO_REQ)
        {
        }

        public string[] GetSvrSubParts()
        {
            try
            {
                int idx = 4;
                return GetStringsAtIndex(ref idx);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error interpreting serverPathParts from message: {exc.Message}");
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
                BitConverter.ToInt32(Bytes, 4);
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
                            out string name, out long fileSize))
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


        public void GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizes(
                    out int seqNr, out bool isSvrWritable, out bool isLast,
                    out string[] folderNames, out string[] fileNames, out long[] fileSizes)
        {
            try
            {
                int idx = 4;

                seqNr = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
                isSvrWritable = BitConverter.ToBoolean(Bytes, idx);
                idx += sizeof(bool);
                folderNames = GetStringsAtIndex(ref idx);
                fileNames = GetFileNamesAndSizesAtIndex(ref idx, out fileSizes);
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
                int idx = 4;
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
                int idx = 4;
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
                BitConverter.ToInt32(Bytes, 4);
        }
        public bool IsLastPart
        {
            get =>
                BitConverter.ToBoolean(Bytes, 4 + sizeof(int));
        }

        public void GetSeqnrAndIslastAndData(out int seqNr, out bool isLast, out byte[] data)
        {
            try
            {
                int idx = 4;
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
        public MsgSvrClCopyToSvrConfirmation(CopyCounters nums, int numErrMsgs)
            : base(MsgSvrClType.COPY_TOSVRCONFIRM)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(nums.FoldersCreated));
            lisBytes.AddRange(BitConverter.GetBytes(nums.FilesCreated));
            lisBytes.AddRange(BitConverter.GetBytes(nums.FilesOverwritten));
            lisBytes.AddRange(BitConverter.GetBytes(nums.FilesSkipped));
            lisBytes.AddRange(BitConverter.GetBytes(nums.DtProblems));
            lisBytes.AddRange(BitConverter.GetBytes(numErrMsgs));
            Bytes = lisBytes.ToArray();
        }


        public MsgSvrClCopyToSvrConfirmation(byte[] bytes)
            : base(bytes, MsgSvrClType.COPY_TOSVRCONFIRM)
        {
        }


        public void GetNums(out CopyCounters nums, out int numErrMsgs)
        {
            try
            {
                nums = new CopyCounters();

                int idx = 4;
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
                numErrMsgs = BitConverter.ToInt32(Bytes, idx);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error getting numbers from message: {exc.Message}");
            }
        }

    }






}

