using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//JEEWEE
//using Windows.UI.StartScreen;

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
        COPY_REQ,
        COPYNEXT_REQ,
        COPY_ANS,
    }

    public class MsgSvrClBase
    {
        public const int BUFSIZEMOREORLESS = 20000;

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
        /// JEEWEE
        /// </summary>
        /// <param name="type"></param>
        protected MsgSvrClBase(MsgSvrClType type)
        {
            Bytes = BitConverter.GetBytes((int)type);
        }


        /// <summary>
        /// JEEWEE
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

                case MsgSvrClType.COPY_REQ:
                    return new MsgSvrClCopyRequest(bytes);

                case MsgSvrClType.COPYNEXT_REQ:
                    return new MsgSvrClCopyNextpartRequest(bytes);

                case MsgSvrClType.COPY_ANS:
                    return new MsgSvrClCopyAnswer(bytes);

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

        public MsgSvrClPathInfoAnswer(int seqNr, PathInfoAnswerState pathInfoAnswerState)
            : base(MsgSvrClType.PATHINFO_ANS)
        {
            int numFolders = 0;
            int numFiles = 0;

            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(seqNr));

            int idxNumFiles = -1;
            int idxNumFolders = lisBytes.Count;
            lisBytes.AddRange(BitConverter.GetBytes(0));    // temp number 0 will be replaced

            bool prevIsDir = true;
            while (pathInfoAnswerState.GetNextFileOrFolder(out bool isDir,
                            out string name, out long fileSize))
            {
                if (prevIsDir && ! isDir)
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
                if (lisBytes.Count >= BUFSIZEMOREORLESS)
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


        public void GetSeqnrAndIslastAndFolderAndFileNamesAndSizes(out int seqNr, out bool isLast,
                    out string[] folderNames, out string[] fileNames, out long[] fileSizes)
        {
            try
            {
                int idx = 4;

                seqNr = BitConverter.ToInt32(Bytes, idx);
                idx += sizeof(int);
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


        public void GetSvrSubPartsAndFolderAndFileNames(out string[] svrSubParts,
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
    /// Answer with copydata, probably a part
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


        public MsgSvrClCopyAnswer(byte[] bytes)
            : base(bytes, MsgSvrClType.COPY_ANS)
        {
        }


        public int SeqNr { get =>
                BitConverter.ToInt32(Bytes, 4); }
        public bool IsLastPart { get =>
                BitConverter.ToBoolean(Bytes, 4 + sizeof(int)); }
        
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
                    $"Error interpreting strings from message: {exc.Message}");
            }
        }
    }
}

