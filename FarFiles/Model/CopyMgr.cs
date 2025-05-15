//JEEWEE
//using AudioToolbox;
using FarFiles.Services;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//JEEWEE
//using static CoreFoundation.DispatchSource;

namespace FarFiles.Model
{
    public enum StartCode
    {
        ERROR,          // followed by string (errMsg)
        FOLDER,         // followed by string (relativepath incl name), DateTime (Creation), DataTime (LastWrite)
        FILE,           // followed by string (name), long (size), int (FileAttr), DateTime (Creation), DataTime (LastWrite), compressedparts
        COMPRESSEDPART, // followed by int (numberof bytes), bytes
    }

    public class CopyMgr : IDisposable
    {
        protected FileDataService _fileDataService;
        //JEEWEE
        //protected string[] _folderNamesToCopy;
        //protected string[] _fileNamesToCopy;
        //protected string _currPathOnSvr;
        //protected string _currRelPathOnClient;
        //JEEWEE
        //protected FileOrFolderData[] _allFileDataInCurrPath;
        //protected int _idxFolders = 0;
        //protected int _idxFiles = 0;

        protected BinaryWriter _writer = null;
        protected BinaryReader _reader = null;
        protected const int BUFSIZEMOREORLESS = 2000;
        protected const int REMAININGLIMIT = 100;
        protected int _bufSizeMoreOrLess = BUFSIZEMOREORLESS;
        protected int _remainingLimit = REMAININGLIMIT;
        protected List<byte> _bufSvrMsg = new List<byte>();
        protected List<NavLevel> _navLevels = new List<NavLevel>();
        protected int _seqNr = 0;
        protected string _currPathOnClient = "";
        protected string _currFileNameOnClient = "";
        protected string _currFileFullPathOnClient = "";
        protected long _currFileSizeOnClient;
        protected long _fileSizeCounter;
        protected FileAttributes _currFileAttrsOnClient;
        protected DateTime _currFileDtCreationOnClient;
        protected DateTime _currFileDtLastWriteOnClient;
        protected Settings _settings;

        //JEEWEE
        //protected byte[] _rdBytesCompress;
        //protected int _bufSvrBytesCompressPos = 0;
        public int NumFoldersCreated { get; protected set; } = 0;
        public int NumFilesCreated { get; protected set; } = 0;
        public int NumFilesOverwritten { get; protected set; } = 0;
        public int NumFilesSkipped { get; protected set; } = 0;
        public int NumErrs { get; protected set; } = 0;
        public List<string> ErrMsgs = new List<string>();  //JEEWEE!!!!!!!!!!!!!!!!!!!!!!! do something

        public CopyMgr(FileDataService fileDataService, Settings alternativeSettings = null,
                int bufSizeMoreOrLess = BUFSIZEMOREORLESS,
                int remainingLimit = REMAININGLIMIT)
        {
            _fileDataService = fileDataService;
            _settings = alternativeSettings ?? MauiProgram.Settings;
            _bufSizeMoreOrLess = bufSizeMoreOrLess;
            _remainingLimit = remainingLimit;
        }

        public void StartCopyFromSvr(MsgSvrClCopyRequest copyRequest)
        {
            CloseThings();
            copyRequest.GetSvrSubPartsAndFolderAndFileNames(out string[] svrSubParts,
                out string[] folderNamesToCopy, out string[] fileNamesToCopy);
            string pathOnSvr = _settings.PathFromRootAndSubParts(svrSubParts);
            string relPathOnClient = "";
            _seqNr = 0;

            //JEEWEE
            //_allFileDataInCurrPath = _fileDataService.GetFilesAndFoldersData(
            //            _currPath, SearchOption.TopDirectoryOnly);

            _navLevels.Clear();
            _navLevels.Add(new NavLevel(pathOnSvr, relPathOnClient,
                    folderNamesToCopy, fileNamesToCopy));
        }


        /// <summary>
        /// Returns a MsgSvrClCopyAnswer or a MsgSvrClErrorAnswer
        /// </summary>
        /// <returns></returns>
        public MsgSvrClBase GetNextPartCopyansFromSvr()
        {
            try
            {
                //JEEWEE
                //if (_bufSvrBytesCompressPos < _rdBytesCompress.Length)
                //    CopyToMsgBufUntilFull(_rdBytesCompress, ref _bufSvrBytesCompressPos);

                bool isLastPart = false;
                NavLevel currNavLevel = _navLevels.Last();
                int numRemaining = _bufSizeMoreOrLess;
                while (numRemaining > 0)
                {
                    if (null != _reader)
                    {
                        if (numRemaining < _remainingLimit)
                            break;
                        byte[] rdBytes = _reader.ReadBytes(numRemaining + numRemaining / 2);
                        if (rdBytes.Length > 0 || _reader.BaseStream.Length == 0)
                        {
                            byte[] compressedBytes = Compress(rdBytes);
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.COMPRESSEDPART));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((int)compressedBytes.Length));
                            _bufSvrMsg.AddRange(compressedBytes);
                        }

                        if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
                        {
                            _reader.Close();
                            _reader = null;
                        }
                    }

                    else if (currNavLevel.CurrIdxFiles < currNavLevel.FileNames.Length)
                    {
                        string fileName = "";

                        try
                        {
                            fileName = currNavLevel.FileNames[currNavLevel.CurrIdxFiles];
                            string fullPath = Path.Combine(currNavLevel.PathOnSvr, fileName);
                            var fData = new FileOrFolderData(fullPath, false, true);
                            _reader = new BinaryReader(File.Open(fullPath,
                                        FileMode.Open, FileAccess.Read));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.FILE));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(fileName));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.FileSize));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((int)fData.Attrs));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtCreation.ToBinary()));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtLastWrite.ToBinary()));
                            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! HASH?
                        }
                        catch (Exception exc)
                        {
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.ERROR));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                                $"Error with copy '{currNavLevel.RelPathOnClient}', file '{fileName}': {exc.Message}"));
                        }
                        currNavLevel.CurrIdxFiles++;
                    }

                    else if (currNavLevel.CurrIdxFolders < currNavLevel.FolderNames.Length)
                    {
                        string folderName = "";
                        bool navlvAdded = false;

                        try
                        {
                            folderName = currNavLevel.FolderNames[currNavLevel.CurrIdxFolders];
                            currNavLevel.CurrIdxFolders++;
                            string fullPathOnSvr = Path.Combine(currNavLevel.PathOnSvr, folderName);
                            string relPathOnClient = Path.Combine(currNavLevel.RelPathOnClient, folderName);

                            var fData = new FileOrFolderData(fullPathOnSvr, true, true);
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.FOLDER));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(relPathOnClient));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtCreation.ToBinary()));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtLastWrite.ToBinary()));

                            _navLevels.Add(new NavLevel(fullPathOnSvr, relPathOnClient));
                            navlvAdded = true;

                            // no exception, so:
                            currNavLevel = _navLevels.Last();
                        }
                        catch (Exception exc)
                        {
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.ERROR));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                                $"Error with copy '{currNavLevel.RelPathOnClient}', folder '{folderName}': {exc.Message}"));
                            if (navlvAdded)
                                _navLevels.RemoveAt(_navLevels.Count - 1);
                        }
                    }
                    else
                    {
                        _navLevels.RemoveAt(_navLevels.Count - 1);
                        if (_navLevels.Count == 0)
                            break;

                        currNavLevel = _navLevels.Last();
                    }

                    numRemaining = _bufSizeMoreOrLess - _bufSvrMsg.Count;
                }

                isLastPart = true;
                this.Dispose();
                return new MsgSvrClCopyAnswer(_seqNr++, isLastPart, _bufSvrMsg.ToArray());
            }
            catch (Exception exc)
            {
                return new MsgSvrClErrorAnswer(
                    $"Server: error copying next part: {exc.Message}");
            }
        }




        /// <summary>
        /// Creates files and folders on Client, copying from server
        /// Returns true if this was the last pat of data
        /// </summary>
        /// <param name="msgSvrClAnswer"></param>
        /// <returns></returns>
        public bool CreateOnClientFromNextPart(MsgSvrClCopyAnswer msgSvrClAnswer)
        {
            msgSvrClAnswer.GetSeqnrAndIslastAndData(out int seqNr, out bool isLast,
                                out byte[] data);
            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! CHECK ON _seqNr
            //AND HASH, AND FILESIZE

            if (seqNr == 0)
            {
                _currPathOnClient = _settings.FullPathRoot;
            }

            int idxData = 0;
            while (true)
            {
                short code = BitConverter.ToInt16(data, idxData);
                idxData += sizeof(short);
                switch (code)
                {
                    case (short)StartCode.ERROR:
                        NumErrs++;
                        string errMsg = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        ErrMsgs.Add(errMsg);
                        break;

                    case (short)StartCode.FOLDER:
                        string relPathOnClient = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        DateTime dtCreation = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);
                        DateTime dtLastWrite = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);

                        _currPathOnClient = Path.Combine(_settings.FullPathRoot, relPathOnClient);
                        if (! Directory.Exists(_currPathOnClient))
                        {
                            Directory.CreateDirectory(_currPathOnClient);
                            Directory.SetCreationTime(_currPathOnClient, dtCreation);
                            Directory.SetLastWriteTime(_currPathOnClient, dtLastWrite);
                            NumFoldersCreated++;
                        }
                        break;

                    case (short)StartCode.FILE:
                        _currFileNameOnClient = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        _currFileFullPathOnClient = Path.Combine(
                            _currPathOnClient, _currFileNameOnClient);
                        _currFileSizeOnClient = BitConverter.ToInt64(data, idxData);
                        idxData += sizeof(long);
                        _currFileAttrsOnClient = (FileAttributes)BitConverter.ToInt32(data, idxData);
                        idxData += sizeof(int);
                        _currFileDtCreationOnClient = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);
                        _currFileDtLastWriteOnClient = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);

                        if (_writer != null)        // should not be possible
                        {
                            _writer.Close();
                        }
                        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! SUPPORT "KEEP"
                        _writer = new BinaryWriter(File.Open(_currFileFullPathOnClient,
                                            FileMode.Create));
                        _fileSizeCounter = 0;
                        break;

                    case (short)StartCode.COMPRESSEDPART:
                        int numBytes = BitConverter.ToInt32(data, idxData);
                        idxData += sizeof(int);
                        byte[] decompressed = Decompress(data, idxData, numBytes);
                        idxData += numBytes;
                        if (_writer == null)
                        {
                            ErrMsgs.Add(
                                $"_writer is null! _fileSizeCounter={_fileSizeCounter}, _currFileSizeOnClient={_currFileSizeOnClient}");
                        }
                        else
                        {
                            _writer.Write(decompressed);
                        }
                        _fileSizeCounter += decompressed.Length;
                        if (_fileSizeCounter >= _currFileSizeOnClient)
                        {
                            if (_fileSizeCounter > _currFileSizeOnClient)
                            {
                                ErrMsgs.Add(
                                    $"_fileSizeCounter not exactly ends right! _fileSizeCounter={_fileSizeCounter}, _currFileSizeOnClient={_currFileSizeOnClient}");
                            }
                            if (_writer != null)
                            {
                                _writer.Close();
                                _writer = null;
                            }

                            File.SetAttributes(_currFileFullPathOnClient,
                                            _currFileAttrsOnClient);
                            File.SetCreationTime(_currFileFullPathOnClient,
                                            _currFileDtCreationOnClient);
                            File.SetLastWriteTime(_currFileFullPathOnClient,
                                            _currFileDtLastWriteOnClient);
                        }
                        break;

                    default:
                        ErrMsgs.Add(
                            $"Unknown StartCode ({code}) encountered in data from server!");
                        break;
                }

                if (idxData >= data.Length)
                {
                    if (idxData > data.Length)
                        ErrMsgs.Add(
                            $"idxData ({idxData}) does not exactly end on end data ({data.Length}");
                        return isLast;
                }
            }


        }







        public void Dispose()
        {
            CloseThings();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data)
        {
            // Thanks, https://stackoverflow.com/questions/39191950/how-to-compress-a-byte-array-without-stream-or-system-io

            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.SmallestSize))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data, int idxStart, int count)
        {
            // Thanks, https://stackoverflow.com/questions/39191950/how-to-compress-a-byte-array-without-stream-or-system-io

            MemoryStream input = new MemoryStream(data, idxStart, count);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }


        protected void CloseThings()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }
        }



        protected class NavLevel
        {
            //JEEWEE
            //public NavLevel[] Children { get; set; } = new NavLevel[0];
            public string PathOnSvr { get; protected set; }
            public string RelPathOnClient { get; protected set; }
            public string[] FileNames { get; protected set; }
            public string[] FolderNames { get; protected set; }
            public int CurrIdxFiles { get; set; } = 0;
            public int CurrIdxFolders { get; set; } = 0;


            /// <summary>
            /// Class to contain folders and files to copy, at one level only
            /// </summary>
            /// <param name="pathOnSvr"></param>
            /// <param name="relPathOnClient"></param>
            /// <param name="folderNamesSelection">if null, then all folders in path</param>
            /// <param name="fileNamesSelection">if null, then all files in path</param>
            public NavLevel(string pathOnSvr, string relPathOnClient,
                    string[] folderNamesSelection = null, string[] fileNamesSelection = null)
            {
                PathOnSvr = pathOnSvr;
                RelPathOnClient = relPathOnClient;
                FolderNames = folderNamesSelection ??
                    Directory.GetDirectories(PathOnSvr).Select(f => Path.GetFileName(f)).ToArray(); ;
                FileNames = fileNamesSelection ??
                    Directory.GetFiles(PathOnSvr).Select(f => Path.GetFileName(f)).ToArray();
            }

        }
    }
}
