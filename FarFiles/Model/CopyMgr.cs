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

namespace FarFiles.Model
{
    public enum StartCode
    {
        ERROR,          // followed by string (errMsg)
        FOLDER,         // followed by string (relativepath incl name), DateTime (Creation), DataTime (LastWrite)
        FILE,           // followed by string (name), long (size), int (FileAttr), DateTime (Creation), DataTime (LastWrite), compressedparts
        COMPRESSEDPART, // followed by int (numberof bytes), bytes
        INFOTOTALFILES, // followed by int (total numberof files, not folders)
    }

    public class CopyMgr : IDisposable
    {
        protected FileDataService _fileDataService;
        protected BinaryWriter _writer = null;
        protected BinaryReader _reader = null;
        protected const int REMAININGLIMIT = 100;

        protected int _remainingLimit = REMAININGLIMIT;
        protected List<byte> _bufSvrMsg = new List<byte>();
        protected List<NavLevel> _navLevels = new List<NavLevel>();
        protected int _seqNr = 0;
        protected bool _infoTotalNumFilesAddedOnSvr = false;
        protected string _currPathOnClient = "";
        protected string _currFileNameOnClient = "";
        protected string _currFileFullPathOnClient = "";
        protected bool _currFileExistedBefore = false;
        protected long _currFileSizeOnClient;
        protected long _fileSizeCounter;
        protected FileAttributes _currFileAttrsOnClient;
        protected DateTime _currFileDtCreationOnClient;
        protected DateTime _currFileDtLastWriteOnClient;
        protected Settings _settings;

        public int TotalNumFilesToCopyOnSvr { get; protected set; } = 0;
        public int NumFilesOpenedOnSvr { get; protected set; } = 0;
        public int ClientInfoTotalNumFilesToCopy { get; protected set; } = 0;
        public bool ClientAborted { get; protected set; } = false;
        public int NumFoldersCreated { get; protected set; } = 0;
        public int NumFilesCreated { get; protected set; } = 0;
        public int NumFilesOverwritten { get; protected set; } = 0;
        public int NumFilesSkipped { get; protected set; } = 0;
        public int NumDtProblems { get; protected set; } = 0;

        public List<string> ErrMsgs = new List<string>();  //JEEWEE!!!!!!!!!!!!!!!!!!!!!!! do something

        public CopyMgr(FileDataService fileDataService, Settings alternativeSettings = null,
                int remainingLimit = REMAININGLIMIT)
        {
            _fileDataService = fileDataService;
            _settings = alternativeSettings ?? MauiProgram.Settings;
            _remainingLimit = remainingLimit;
        }

        public void StartCopyFromSvr(MsgSvrClCopyRequest copyRequest)
        {
            CloseThings();
            copyRequest.GetSvrSubPartsAndFolderAndFileNames(out string[] svrSubParts,
                out string[] folderNamesToCopy, out string[] fileNamesToCopy);
            _seqNr = 0;
            TotalNumFilesToCopyOnSvr = CalcTotalNumFilesToCopy(svrSubParts,
                                folderNamesToCopy, fileNamesToCopy);
            NumFilesOpenedOnSvr = 0;
            _infoTotalNumFilesAddedOnSvr = false;

            _navLevels.Clear();
            _navLevels.Add(new NavLevel(_fileDataService, _settings, svrSubParts,
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
                bool isLastPart = false;
                NavLevel currNavLevel = _navLevels.Last();
                _bufSvrMsg.Clear();
                int numRemaining = _settings.BufSizeMoreOrLess;

                while (numRemaining > 0)
                {
                    if (!_infoTotalNumFilesAddedOnSvr)
                    {
                        _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.INFOTOTALFILES));
                        _bufSvrMsg.AddRange(BitConverter.GetBytes(TotalNumFilesToCopyOnSvr));
                        _infoTotalNumFilesAddedOnSvr = true;
                    }

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
                            _reader = _fileDataService.OpenBinaryReaderGeneric(
                                    _settings.FullPathRoot, _settings.AndroidUriRoot,
                                    currNavLevel.SvrSubParts, fileName,
                                    out FileOrFolderData fData);
                            NumFilesOpenedOnSvr++;

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
                                $"Error with copy, relpath: '{currNavLevel.JoinedSubPartsOnClient}', file '{fileName}': {exc.Message}"));
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
                            
                            var fData = _fileDataService.NewFileOrFolderDataGeneric(_settings.FullPathRoot,
                                    _settings.AndroidUriRoot, currNavLevel.SvrSubParts, folderName, true);
                            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! fData may contain only exception, what then?
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.FOLDER));

                            var newNavLevel = new NavLevel(currNavLevel, folderName);
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                                        newNavLevel.JoinedSubPartsOnClient));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtCreation.ToBinary()));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtLastWrite.ToBinary()));

                            _navLevels.Add(newNavLevel);
                            navlvAdded = true;

                            // no exception, so:
                            currNavLevel = newNavLevel;
                        }
                        catch (Exception exc)
                        {
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.ERROR));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                                $"Error with copy, relpath: '{currNavLevel.JoinedSubPartsOnClient}', folder '{folderName}': {exc.Message}"));
                            if (navlvAdded)
                                _navLevels.RemoveAt(_navLevels.Count - 1);
                        }
                    }
                    else
                    {
                        _navLevels.RemoveAt(_navLevels.Count - 1);
                        if (_navLevels.Count == 0)
                        {
                            isLastPart = true;
                            this.Dispose();
                            break;
                        }

                        currNavLevel = _navLevels.Last();
                    }

                    numRemaining = _settings.BufSizeMoreOrLess - _bufSvrMsg.Count;
                }

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
        /// Returns true if this was the last part of data
        /// </summary>
        /// <param name="msgSvrClAnswer"></param>
        /// <param name="funcCopyGetAbortSetLbls">returns true if client user aborted</param>
        /// <returns></returns>
        public bool CreateOnClientFromNextPart(MsgSvrClCopyAnswer msgSvrClAnswer,
                    Func<int,int,long,long,bool> funcCopyGetAbortSetLbls = null)
        {
            msgSvrClAnswer.GetSeqnrAndIslastAndData(out int seqNr, out bool isLast,
                                out byte[] data);
            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! NOT YET ANDROID
            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! CHECK ON _seqNr
            //AND HASH, AND FILESIZE

            if (seqNr == 0)
            {
                _currPathOnClient = _settings.FullPathRoot;
            }

            if (data.Length < sizeof(short))
                return isLast;

            int idxData = 0;
            while (true)
            {
                short code = BitConverter.ToInt16(data, idxData);
                idxData += sizeof(short);
                switch (code)
                {
                    case (short)StartCode.ERROR:
                        string errMsg = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        ErrMsgs.Add(errMsg);
                        break;

                    case (short)StartCode.FOLDER:
                        string joinedPartsRelPathOnClient = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        string[] partsRelPathOnClient = joinedPartsRelPathOnClient.Split('/'); 
                        DateTime dtCreation = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);
                        DateTime dtLastWrite = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);

#if ANDROID
                        _currPathOnClient = "JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!";
#else
                        _currPathOnClient = FileDataService.PathFromRootAndSubPartsWindows(
                                _settings.FullPathRoot, partsRelPathOnClient);
#endif
                        if (! Directory.Exists(_currPathOnClient))
                        {
                            Directory.CreateDirectory(_currPathOnClient);
                            NumDtProblems += _fileDataService.SetDateTimesGeneric(_currPathOnClient,
                                true, dtCreation, dtLastWrite);
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

                        if (_writer != null)
                        {
                            _writer.Close();
                            _writer = null;
                        }

                        _currFileExistedBefore = File.Exists(_currFileFullPathOnClient);
                        if (_currFileExistedBefore && 1 == _settings.Idx0isOverwr1isSkip)
                        {
                            NumFilesSkipped++;
                            // and _writer stays null
                        }
                        else
                        {
                            try
                            {
                                _writer = new BinaryWriter(File.Open(_currFileFullPathOnClient,
                                                    FileMode.Create));
                                NumFilesCreated++;
                                if (_currFileExistedBefore)
                                    NumFilesOverwritten++;
                            }
                            catch (Exception exc)
                            {
                                ErrMsgs.Add(
                                    $"Could not create '{_currFileFullPathOnClient}': {exc.Message}");
                                _writer = null;
                            }
                        }
                        _fileSizeCounter = 0;

                        if (null != funcCopyGetAbortSetLbls)
                        {
                            if (funcCopyGetAbortSetLbls(NumFilesSkipped + NumFilesCreated,
                                    ClientInfoTotalNumFilesToCopy,
                                    0, _currFileSizeOnClient))
                            {
                                ClientAbort();
                            }
                        }
                        break;

                    case (short)StartCode.COMPRESSEDPART:
                        int numBytes = BitConverter.ToInt32(data, idxData);
                        idxData += sizeof(int);
                        byte[] decompressed = Decompress(data, idxData, numBytes);
                        idxData += numBytes;
                        if (_writer == null)
                        {
                            // file is skipped or could not be created
                        }
                        else
                        {
                            _writer.Write(decompressed);
                        }
                        _fileSizeCounter += decompressed.Length;

                        if (null != funcCopyGetAbortSetLbls)
                        {
                            if (funcCopyGetAbortSetLbls(NumFilesSkipped + NumFilesCreated,
                                    ClientInfoTotalNumFilesToCopy,
                                    _fileSizeCounter, _currFileSizeOnClient))
                            {
                                ClientAbort();
                                break;
                            }
                        }

                        if (_fileSizeCounter >= _currFileSizeOnClient)
                        {
                            if (_fileSizeCounter > _currFileSizeOnClient)
                            {
                                ErrMsgs.Add(
                                    $"_fileSizeCounter not exactly ends right! _fileSizeCounter={_fileSizeCounter}, _currFileSizeOnClient={_currFileSizeOnClient}");
                            }
                            if (_writer != null)    // else: file is skipped or could not be created
                            {
                                _writer.Close();
                                _writer = null;

                                File.SetAttributes(_currFileFullPathOnClient,
                                                _currFileAttrsOnClient);
                                NumDtProblems += _fileDataService.SetDateTimesGeneric(_currFileFullPathOnClient,
                                    false, _currFileDtCreationOnClient, _currFileDtLastWriteOnClient);
                            }
                        }
                        break;

                    case (short)StartCode.INFOTOTALFILES:
                        ClientInfoTotalNumFilesToCopy = BitConverter.ToInt32(data, idxData);
                        idxData += sizeof(int);
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



        protected void ClientAbort()
        {
            ClientAborted = true;
            if (null != _writer)        // so: file was created, not skipped, and is now being written
            {
                _writer.Close();
                _writer = null;
                File.Delete(_currFileFullPathOnClient);     // let no partially created files remain
                NumFilesCreated--;
                if (_currFileExistedBefore)
                    NumFilesOverwritten--;
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



        public static T[] AddOneToArray<T>(IEnumerable<T> source, T add)
        {
            var retList = source.ToList();
            retList.Add(add);
            return retList.ToArray();
        }


        protected int CalcTotalNumFilesToCopy(string[] svrSubParts,
                            string[] folderNamesToCopy, string[] fileNamesToCopy)
        {
            int totalNum = fileNamesToCopy.Length;
            foreach (string folderName in folderNamesToCopy)
            {
                totalNum += _fileDataService.CalcTotalNumFilesOfFolderWithSubfolders(
                        _settings.FullPathRoot, _settings.AndroidUriRoot,
                        svrSubParts, folderName);
            }

            return totalNum;
        }




        /// <summary>
        /// Class for serverside, to contain folders and files to copy, at one level only
        /// </summary>
        protected class NavLevel
        {
            protected FileDataService _fileDataService;
            protected Settings _settings;
            public string[] SvrSubParts { get; protected set; }
            public int IdxStartClientpathInSvrSubParts { get; protected set; }
            public string[] FileNames { get; protected set; }
            public string[] FolderNames { get; protected set; }
            public int CurrIdxFiles { get; set; } = 0;
            public int CurrIdxFolders { get; set; } = 0;


            public string PathOnSvrWindows { get => _settings.PathFromRootAndSubPartsWindows(
                        SvrSubParts);
            }

            public string JoinedSubPartsOnClient { get => String.Join("/", SvrSubParts,
                    IdxStartClientpathInSvrSubParts,
                    SvrSubParts.Length - IdxStartClientpathInSvrSubParts); }



            /// <summary>
            /// Ctor for first NavLevel to be created, at level that was selected by client
            /// </summary>
            /// <param name="fileDataService"></param>
            /// <param name="settings"></param>
            /// <param name="svrSubParts">on client selected server path; client top path starts here</param>
            /// <param name="folderNamesSelection">on client selected folders to copy</param>
            /// <param name="fileNamesSelection">on client selected files to copy</param>
            public NavLevel(FileDataService fileDataService, Settings settings,
                    string[] svrSubParts,
                    string[] folderNamesSelection, string[] fileNamesSelection)
            {
                _fileDataService = fileDataService;
                _settings = settings;
                SvrSubParts = svrSubParts;
                IdxStartClientpathInSvrSubParts = svrSubParts.Length;

                FolderNames = folderNamesSelection;
                FileNames = fileNamesSelection;
            }


            /// <summary>
            /// Ctor to create the NavLevel for a folder on a below level
            /// </summary>
            /// <param name="navLevelParent"></param>
            /// <param name="folderName"></param>
            public NavLevel(NavLevel navLevelParent, string folderName)
            {
                _fileDataService = navLevelParent._fileDataService;
                _settings = navLevelParent._settings;
                SvrSubParts = AddOneToArray(navLevelParent.SvrSubParts, folderName);
                IdxStartClientpathInSvrSubParts = navLevelParent.IdxStartClientpathInSvrSubParts;

                FolderNames = _fileDataService.GetDirFolderNamesGeneric(
                            _settings.FullPathRoot,
                            _settings.AndroidUriRoot, SvrSubParts);
                FileNames = _fileDataService.GetDirFileNamesGeneric(
                            _settings.FullPathRoot,
                            _settings.AndroidUriRoot, SvrSubParts);
            }
        }
    }
}
