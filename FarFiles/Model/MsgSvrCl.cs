using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public enum MsgSvrClType
    {
        ROOTINFO_REQ,
        ROOTINFO_ANS,
    }

    public class MsgSvrCl
    {
        public List<byte> Bytes;

        public MsgSvrClType Type { get; set; }

        public MsgSvrCl(MsgSvrClType type)
        {
            Type = type;
            Bytes = new List<byte>();
            Bytes.AddRange(BitConverter.GetBytes((int)type));
        }

        public static MsgSvrCl MsgSvrClCreateRootInfoReq()
        {
            return new MsgSvrCl(MsgSvrClType.ROOTINFO_REQ);
        }

        public static MsgSvrCl MsgSvrClCreateRootInfoAns(IEnumerable<string> folderNames,
                            IEnumerable<string> fileNames)
        {
            MsgSvrCl ret = new MsgSvrCl(MsgSvrClType.ROOTINFO_ANS);

            ret.Bytes.AddRange(BitConverter.GetBytes(folderNames.Count()));
            foreach (string folderName in folderNames)
            {
                ret.Bytes.AddRange(StrPlusLenToBytes(folderName));
            }
            ret.Bytes.AddRange(BitConverter.GetBytes(fileNames.Count()));
            foreach (string fileName in fileNames)
            {
                ret.Bytes.AddRange(StrPlusLenToBytes(fileName));
            }

            return ret;
        }


        public static MsgSvrCl MsgSvrClFromBytes(byte[] bytes)
        {
            int iType = -1;
            try
            {
                iType = BitConverter.ToInt32(bytes, 0);
                return new MsgSvrCl((MsgSvrClType)iType);
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


        public static string StrPlusLenFromBytes(byte[] bytes)
        {
            if (bytes.Length < 4)
                throw new Exception(
                    $"PROGRAMMERS: StrPlusLenFromBytes: Length ({bytes.Length}) less than 4 bytes");

            int strLen = BitConverter.ToInt32(bytes, 0);
            if (bytes.Length < 4 + strLen)
                throw new Exception(
                    $"PROGRAMMERS: StrPlusLenFromBytes: Length ({bytes.Length}) less than 4+{strLen} bytes");

            string retStr = "";
            for (int i = 4; i < bytes.Length; i++)
            {
                retStr += (char)bytes[i];
            }

            return retStr;
        }
    }
}
