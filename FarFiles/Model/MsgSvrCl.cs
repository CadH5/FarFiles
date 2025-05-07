using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public enum MsgSvrClType
    {
        ERROR,
        STRING_SEND,
        STRING_ANS,
        ROOTINFO_REQ,
        ROOTINFO_ANS,
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

        //JEEWEE: I THINK WE DONT NEED TypeAnswer
        public MsgSvrClType? TypeAnswer { get; protected set; } = null;


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
                throw new Exception($"Expected message type: {expectedType}, got: {foundType}");
            Bytes = bytes;      // references, no copy!
        }


        public static MsgSvrClBase CreateFromBytes(byte[] bytes)
        {
            MsgSvrClType type = TypeFromBytes(bytes);
            switch (type)
            {
                case MsgSvrClType.STRING_SEND:
                    return new MsgSvrClStringSend(bytes);

                case MsgSvrClType.STRING_ANS:
                    return new MsgSvrClStringAnswer(bytes);

                case MsgSvrClType.ROOTINFO_REQ:
                    return new MsgSvrClRootInfoRequest(bytes);

                case MsgSvrClType.ROOTINFO_ANS:
                    return new MsgSvrClRootInfoAnswer(bytes);

                default:
                    throw new Exception(
                        $"PROGRAMMERS: CreateFromBytes(): not handled type {type}");
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
            int till = indexStart + strLen;
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
            TypeAnswer = MsgSvrClType.STRING_ANS;
        }

        public MsgSvrClStringSend(byte[] bytes)
            : base(bytes, MsgSvrClType.STRING_SEND)
        {
            TypeAnswer = MsgSvrClType.STRING_ANS;
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
    /// MsgSvrClRootInfoRequest
    /// </summary>
    public class MsgSvrClRootInfoRequest : MsgSvrClBase
    {
        public MsgSvrClRootInfoRequest() : base(MsgSvrClType.ROOTINFO_REQ)
        {
            TypeAnswer = MsgSvrClType.ROOTINFO_ANS;
        }

        public MsgSvrClRootInfoRequest(byte[] bytes)
            : base(bytes, MsgSvrClType.ROOTINFO_REQ)
        {
        }
    }




    /// <summary>
    /// MsgSvrClRootInfoAnswer
    /// </summary>
    public class MsgSvrClRootInfoAnswer : MsgSvrClBase
    {
        public MsgSvrClRootInfoAnswer(IEnumerable<string> folderNames,
                            IEnumerable<string> fileNames)
            : base(MsgSvrClType.ROOTINFO_ANS)
        {
            var lisBytes = Bytes.ToList();
            lisBytes.AddRange(BitConverter.GetBytes(folderNames.Count()));
            foreach (string folderName in folderNames)
            {
                lisBytes.AddRange(StrPlusLenToBytes(folderName));
            }
            lisBytes.AddRange(BitConverter.GetBytes(fileNames.Count()));
            foreach (string fileName in fileNames)
            {
                lisBytes.AddRange(StrPlusLenToBytes(fileName));
            }
            Bytes = lisBytes.ToArray();
        }


        public MsgSvrClRootInfoAnswer(byte[] bytes)
            : base(bytes, MsgSvrClType.ROOTINFO_ANS)
        {
        }



        public void GetFolderAndFileNames(out string[] folderNames, out string[] fileNames)
        {
            int numFolders = 0;
            int numFiles = 0;

            try
            {
                int i, idx = 4;
                numFolders = BitConverter.ToInt32(Bytes, idx);
                folderNames = new string[numFolders];
                idx += 4;
                for (i = 0; i < numFolders; i++)
                {
                    folderNames[i] = StrPlusLenFromBytes(Bytes, ref idx);
                }
                numFiles = BitConverter.ToInt32(Bytes, idx);
                fileNames = new string[numFiles];
                idx += 4;
                for (i = 0; i < numFiles; i++)
                {
                    fileNames[i] = StrPlusLenFromBytes(Bytes, ref idx);
                }
            }
            catch (Exception exc)
            {
                throw new Exception(
                    $"Error interpreting folder- and filenames from message: {exc.Message}");
            }
        }
    }
}
