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
        //JEEWEE
        //ERROR,          // followed by string (errMsg)
        
        CDPATH,         // followed by string (relativepath to cd to)
        FOLDER,         // followed by string (relativepath incl name), DateTime (Creation), DataTime (LastWrite)
        FILE,           // followed by string (name), long (size), int (FileAttr), DateTime (Creation), DataTime (LastWrite), compressedparts
        COMPRESSEDPART, // followed by int (numberof bytes), bytes
        INFOTOTALFILES, // followed by int (total numberof files, not folders)
        IDX0OVERWR1SKIP,// followed by int (override settings value)
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
        protected int _idx0isOverwr1isSkip = 0;
        protected int _seqNr = 0;
        protected bool _startInfoAddedOnSrc = false;
        protected string[] _currPartsRelPathOnDest = new string[0];
        protected string _currPathOnDestWin = "";
        protected string _currFileNameOnDestOrSrc = "";
        protected string _currFileFullPathOnDestWin = "";
        protected bool _currFileExistedBefore = false;
        protected long _currFileSizeOnDestOrSrc;
        protected int _currNumFilesOpenedOnSrc;
        protected long _fileSizeCounter;
        protected IncrementalHash _readHasher = null;
        protected IncrementalHash _writeHasher = null;
        protected FileAttributes _currFileAttrsOnDest;
        protected DateTime _currFileDtCreationOnDest;
        protected DateTime _currFileDtLastWriteOnDest;
        protected Settings _settings;

        public int NumFilesOpenedOnSrc { get; protected set; } = 0;
        public int ClientTotalNumFilesToCopyFromOrTo { get; protected set; } = 0;
        public bool ClientAborted { get; protected set; } = false;

        //JEEWEE
        //public int NumFoldersCreated { get; protected set; } = 0;
        //public int NumFilesCreated { get; protected set; } = 0;
        //public int NumFilesOverwritten { get; protected set; } = 0;
        //public int NumFilesSkipped { get; protected set; } = 0;
        //public int NumDtProblems { get; protected set; } = 0;
        public CopyCounters Nums { get; protected set; } = new CopyCounters();

        public List<string> ErrMsgs = new List<string>();
        public string FirstErrMsg { get => 0 == ErrMsgs.Count ? "" : ErrMsgs.First(); }

        public CopyMgr(FileDataService fileDataService, Settings alternativeSettings = null,
                int remainingLimit = REMAININGLIMIT)
        {
            _fileDataService = fileDataService;
            _settings = alternativeSettings ?? MauiProgram.Settings;
            _idx0isOverwr1isSkip = _settings.Idx0isOverwr1isSkip;   // on server, may be overwritten by value on client
            _remainingLimit = remainingLimit;
        }

        public void StartCopyFromOrToSvrOnSvrOrClient(MsgSvrClCopyRequest copyRequest,
                string[] clientSubPartsIfClientToSvrOrNull = null)
        {
            CloseThings();
            copyRequest.GetSubPartsAndFolderAndFileNames(out string[] svrSubParts,
                out string[] folderNamesToCopy, out string[] fileNamesToCopy);
            _seqNr = 0;
            ClientTotalNumFilesToCopyFromOrTo = CalcTotalNumFilesToCopy(
                    clientSubPartsIfClientToSvrOrNull ?? svrSubParts,
                    folderNamesToCopy, fileNamesToCopy);
            NumFilesOpenedOnSrc = 0;
            _startInfoAddedOnSrc = false;

            _navLevels.Clear();
            _navLevels.Add(new NavLevel(_fileDataService, _settings, svrSubParts,
                    clientSubPartsIfClientToSvrOrNull,
                    folderNamesToCopy, fileNamesToCopy));
        }


        /// <summary>
        /// Returns a MsgSvrClCopyAnswer, MsgSvrClCopyToSvrPart or MsgSvrClErrorAnswer
        /// </summary>
        /// <param name="clientToSvr">if true, returns a MsgSvrClCopyToSvrPart instead of MsgSvrClCopyAnswer</param>
        /// <param name="funcCopyGetAbortSetLbls">returns true if client user aborted (intended for client TO server)</param>
        /// <returns></returns>
        public MsgSvrClBase GetNextPartCopyansFromSrc(bool clientToSvr,
                Func<int, int, long, long, bool> funcCopyGetAbortSetLbls = null)
        {
            try
            {
                bool isLastPart = false;
                NavLevel currNavLevel = _navLevels.Last();
                _bufSvrMsg.Clear();
                int numRemaining = _settings.BufSizeMoreOrLess;

                if (null != funcCopyGetAbortSetLbls)
                {
                    if (funcCopyGetAbortSetLbls(_currNumFilesOpenedOnSrc,
                            ClientTotalNumFilesToCopyFromOrTo,
                            _fileSizeCounter, _currFileSizeOnDestOrSrc))
                    {
                        ClientAbort(true);
                    }
                }

                while (numRemaining > 0)
                {
                    if (!_startInfoAddedOnSrc)
                    {
                        // total numbers of files to copy, for ui progress indicators
                        _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.INFOTOTALFILES));
                        _bufSvrMsg.AddRange(BitConverter.GetBytes(ClientTotalNumFilesToCopyFromOrTo));

                        if (clientToSvr)
                        {
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.IDX0OVERWR1SKIP));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(_settings.Idx0isOverwr1isSkip));
                        }

                        // if copying TO server, server must know it's start path in case
                        // it's not the root and the copy does not start with a folder
                        if (clientToSvr && currNavLevel.SvrSubParts.Length > 0)
                        {
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.CDPATH));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                                            currNavLevel.JoinedSubPartsOnSvr));
                        }

                        _startInfoAddedOnSrc = true;
                    }

                    if (null != _reader)
                    {
                        if (numRemaining < _remainingLimit)
                            break;
                        byte[] rdBytes = _reader.ReadBytes(numRemaining + numRemaining / 2);
                        if (rdBytes.Length > 0 || _reader.BaseStream.Length == 0)
                        {
                            _readHasher.AppendData(rdBytes);
                            byte[] compressedBytes = Compress(rdBytes);
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.COMPRESSEDPART));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((int)compressedBytes.Length));
                            _bufSvrMsg.AddRange(compressedBytes);
                        }
                        _fileSizeCounter += rdBytes.Length;

                        if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
                        {
                            if (_fileSizeCounter != _currFileSizeOnDestOrSrc)
                                ErrMsgs.Add(
                                    $"file '{_currFileNameOnDestOrSrc}': end of stream, but _fileSizeCounter={_fileSizeCounter} and _currFileSizeOnDestOrSrc={_currFileSizeOnDestOrSrc}");
                            CloseReaderIfNotNull();

                            byte[] finalHash = _readHasher.GetHashAndReset();
                            _readHasher = null;
                            string hashHex = BitConverter.ToString(finalHash).ToLowerInvariant();
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(hashHex));
                        }
                    }

                    else if (currNavLevel.CurrIdxFiles < currNavLevel.FileNames.Length)
                    {
                        _currFileNameOnDestOrSrc = "";

                        try
                        {
                            _currFileNameOnDestOrSrc = currNavLevel.FileNames[currNavLevel.CurrIdxFiles];
                            string[] pathParts = clientToSvr ?
                                    currNavLevel.ClientSubPartsOrNull :
                                    currNavLevel.SvrSubParts;
                            _reader = _fileDataService.OpenBinaryReaderGeneric(
                                    _settings.FullPathRoot, _settings.AndroidUriRoot,
                                    pathParts, _currFileNameOnDestOrSrc,
                                    out FileOrFolderData fData);
                            _readHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                            _currNumFilesOpenedOnSrc++;
                            _currFileSizeOnDestOrSrc = fData.FileSize;
                            _fileSizeCounter = 0;

                            MauiProgram.Log.LogLine(
                                $"source: reader opened: '{_settings.FullPathRoot}', '" +
                                String.Join('/', pathParts) + $"', '{_currFileNameOnDestOrSrc}'");

                            NumFilesOpenedOnSrc++;

                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.FILE));
                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(_currFileNameOnDestOrSrc));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.FileSize));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((int)fData.Attrs));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtCreation.ToBinary()));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtLastWrite.ToBinary()));
                        }
                        catch (Exception exc)
                        {
                            //JEEWEE
                            //_bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.ERROR));
                            //_bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                            //    $"Error with copy, relpath: '{currNavLevel.JoinedSvrSubPartsVisibleToClient}', file '{fileName}': {exc.Message}"));
                            ErrMsgs.Add(
                                $"Error with copy, relpath: '{currNavLevel.JoinedSvrSubPartsVisibleToClient}', file '{_currFileNameOnDestOrSrc}': {exc.Message}");
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

                            string[] subPartsSrc = clientToSvr ?
                                    currNavLevel.ClientSubPartsOrNull :
                                    currNavLevel.SvrSubParts;

                            var fData = _fileDataService.NewFileOrFolderDataGeneric(_settings.FullPathRoot,
                                        _settings.AndroidUriRoot, subPartsSrc, folderName, true);
                            _bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.FOLDER));

                            var newNavLevel = new NavLevel(currNavLevel, folderName, clientToSvr);
                            string joinedSubPartsDest = clientToSvr ?
                                    newNavLevel.JoinedSubPartsOnSvr :
                                    newNavLevel.JoinedSvrSubPartsVisibleToClient;

                            _bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                                    joinedSubPartsDest));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtCreation.ToBinary()));
                            _bufSvrMsg.AddRange(BitConverter.GetBytes(fData.DtLastWrite.ToBinary()));

                            _navLevels.Add(newNavLevel);
                            navlvAdded = true;

                            // no exception, so:
                            currNavLevel = newNavLevel;
                        }
                        catch (Exception exc)
                        {
                            //JEEWEE
                            //_bufSvrMsg.AddRange(BitConverter.GetBytes((short)StartCode.ERROR));
                            //_bufSvrMsg.AddRange(MsgSvrClBase.StrPlusLenToBytes(
                            //    $"Error with copy, relpath: '{currNavLevel.JoinedSvrSubPartsVisibleToClient}', folder '{folderName}': {exc.Message}"));

                            ErrMsgs.Add(
                                $"Error with copy, relpath: '{currNavLevel.JoinedSvrSubPartsVisibleToClient}', folder '{folderName}': {exc.Message}");

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

                return clientToSvr ?
                    new MsgSvrClCopyToSvrPart(_seqNr++, isLastPart, _bufSvrMsg.ToArray()) :
                    new MsgSvrClCopyAnswer(_seqNr++, isLastPart, _bufSvrMsg.ToArray());
            }
            catch (Exception exc)
            {
                return new MsgSvrClErrorAnswer(
                    $"Source: error copying next part: {exc.Message}");
            }
        }




        /// <summary>
        /// Creates files and folders on destination (client or Server), copying from source
        /// Returns true if this was the last part of data
        /// </summary>
        /// <param name="msgSvrClAnswer">this may be type MsgSvrClCopyAnswer or MsgSvrClCopyToSvrPart</param>
        /// <param name="funcCopyGetAbortSetLbls">returns true if client user aborted</param>
        /// <returns></returns>
        public bool CreateOnDestFromNextPart(MsgSvrClCopyAnswer msgSvrClAnswer,
                    Func<int,int,long,long,bool> funcCopyGetAbortSetLbls = null)
        {
            msgSvrClAnswer.GetSeqnrAndIslastAndData(out int seqNr, out bool isLast,
                                out byte[] data);

            if (seqNr == 0)
            {
                _currPathOnDestWin = _settings.FullPathRoot;
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
                    //JEEWEE
                    //case (short)StartCode.ERROR:
                    //    string errMsg = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                    //    ErrMsgs.Add(errMsg);
                    //    break;

                    case (short)StartCode.CDPATH:
                    case (short)StartCode.FOLDER:
                        DateTime dtCreation = DateTime.MinValue;
                        DateTime dtLastWrite = DateTime.MinValue;
                        string joinedPartsRelPathOnDest = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        _currPartsRelPathOnDest = joinedPartsRelPathOnDest.Split('/');

                        if (code == (short)StartCode.FOLDER)
                        {
                            dtCreation = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                            idxData += sizeof(long);
                            dtLastWrite = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                            idxData += sizeof(long);
                        }

#if ANDROID
                        _currPathOnDestWin = "";
                        // not possible to set Attributes or LastModified on Android
#else
                        _currPathOnDestWin = FileDataService.PathFromRootAndSubPartsWindows(
                                _settings.FullPathRoot, _currPartsRelPathOnDest);
#endif

                        if (code == (short)StartCode.FOLDER)
                        {
                            if (_fileDataService.CreatePathGeneric(
                                _settings.FullPathRoot, _settings.AndroidUriRoot,
                                _currPartsRelPathOnDest, dtCreation, dtLastWrite,
                                out int dtCreatedProblems))
                            {
                                Nums.FoldersCreated++;
                            }
                            Nums.DtProblems += dtCreatedProblems;

                            //JEEWEE
                            //if (!Directory.Exists(_currPathOnDestWin))
                            //{
                            //    Directory.CreateDirectory(_currPathOnDestWin);
                            //    Nums.DtProblems += _fileDataService.SetDateTimesWindows(_currPathOnDestWin,
                            //        true, dtCreation, dtLastWrite);
                            //    Nums.FoldersCreated++;
                            //}
                        }
                        break;

                    case (short)StartCode.FILE:
                        _currFileNameOnDestOrSrc = MsgSvrClBase.StrPlusLenFromBytes(data, ref idxData);
                        _currFileFullPathOnDestWin = Path.Combine(
                            _currPathOnDestWin, _currFileNameOnDestOrSrc);
                        _currFileSizeOnDestOrSrc = BitConverter.ToInt64(data, idxData);
                        idxData += sizeof(long);
                        _currFileAttrsOnDest = (FileAttributes)BitConverter.ToInt32(data, idxData);
                        idxData += sizeof(int);
                        _currFileDtCreationOnDest = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);
                        _currFileDtLastWriteOnDest = DateTime.FromBinary(BitConverter.ToInt64(data, idxData));
                        idxData += sizeof(long);

                        CloseWriterIfNotNull();

                        //JEEWEE ANDROID
                        //_currFileExistedBefore = File.Exists(_currFileFullPathOnDestWin);
                        //if (_currFileExistedBefore && 1 == _idx0isOverwr1isSkip)
                        //{
                        //    Nums.FilesSkipped++;
                        //    // and _writer stays null
                        //}
                        //else
                        //{
                        try
                        {
                            _writer = _fileDataService.OpenBinaryWriterGeneric(
                                        _settings.FullPathRoot, _settings.AndroidUriRoot,
                                        _currPartsRelPathOnDest, _currFileNameOnDestOrSrc,
                                        1 == _idx0isOverwr1isSkip,
                                        out _currFileExistedBefore);

                            if (_writer != null)    // else: skipped existing file
                            {
                                MauiProgram.Log.LogLine(
                                    $"dest: Windows: writer opened: '{_currFileFullPathOnDestWin}'");
                                Nums.FilesCreated++;
                                if (_currFileExistedBefore)
                                    Nums.FilesOverwritten++;
                                _writeHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                            }
                            else
                            {
                                Nums.FilesSkipped++;
                            }
                        }
                        catch (Exception exc)
                        {
                            ErrMsgs.Add(
                                $"Could not create '{_currFileFullPathOnDestWin}': {exc.Message}");
                            _writer = null;
                        }

                        _fileSizeCounter = 0;       // also important if file is skipped

                        if (null != funcCopyGetAbortSetLbls)
                        {
                            if (funcCopyGetAbortSetLbls(Nums.FilesSkipped + Nums.FilesCreated,
                                    ClientTotalNumFilesToCopyFromOrTo,
                                    _fileSizeCounter, _currFileSizeOnDestOrSrc))
                            {
                                ClientAbort(false);
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
                            _writeHasher.AppendData(decompressed);
                        }
                        _fileSizeCounter += decompressed.Length;

                        if (null != funcCopyGetAbortSetLbls)
                        {
                            if (funcCopyGetAbortSetLbls(Nums.FilesSkipped + Nums.FilesCreated,
                                    ClientTotalNumFilesToCopyFromOrTo,
                                    _fileSizeCounter, _currFileSizeOnDestOrSrc))
                            {
                                ClientAbort(false);
                                break;
                            }
                        }

                        if (_fileSizeCounter >= _currFileSizeOnDestOrSrc)
                        {
                            if (_fileSizeCounter > _currFileSizeOnDestOrSrc)
                            {
                                ErrMsgs.Add(
                                    $"_fileSizeCounter not exactly ends right! _fileSizeCounter={_fileSizeCounter}, _currFileSizeOnDestOrSrc={_currFileSizeOnDestOrSrc}");
                            }

                            // hash: was added, also if we skip the file, so it must
                            // anyway be read, but we ignore the check if we skipped
                            string hashHexExpected = MsgSvrClBase.StrPlusLenFromBytes(
                                        data, ref idxData);

                            if (_writer != null)    // else: file is skipped or could not be created
                            {
                                CloseWriterIfNotNull();

                                byte[] finalHash = _writeHasher.GetHashAndReset();
                                _writeHasher = null;
                                string hashHex = BitConverter.ToString(finalHash).ToLowerInvariant();
                                if (hashHexExpected != hashHex)
                                {
                                    Nums.ErrHashesDiff++;
                                    ErrMsgs.Add($"File '{_currFileNameOnDestOrSrc}': hash check failed");
                                }

#if ANDROID
                                // not possible to set Attributes or LastModified on Android
#else
                                File.SetAttributes(_currFileFullPathOnDestWin,
                                                _currFileAttrsOnDest);
                                Nums.DtProblems += _fileDataService.SetDateTimesWindows(_currFileFullPathOnDestWin,
                                    false, _currFileDtCreationOnDest, _currFileDtLastWriteOnDest);
#endif
                            }
                        }
                        break;

                    case (short)StartCode.INFOTOTALFILES:
                        ClientTotalNumFilesToCopyFromOrTo = BitConverter.ToInt32(data, idxData);
                        idxData += sizeof(int);
                        break;

                    case (short)StartCode.IDX0OVERWR1SKIP:
                        _idx0isOverwr1isSkip = BitConverter.ToInt32(data, idxData);
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


        public void ClientAbort(bool clientToSvr)
        {
            ClientAborted = true;
            if (clientToSvr)
                return;

            if (null != _writer)        // so: file was created, not skipped, and is now being written
            {
                _writer.Close();
                _writer = null;

                // no partially created files should remain
                _fileDataService.DeleteFileGeneric(
                            _settings.FullPathRoot, _settings.AndroidUriRoot,
                            _currPartsRelPathOnDest, _currFileNameOnDestOrSrc);
                Nums.FilesCreated--;
                if (_currFileExistedBefore)
                    Nums.FilesOverwritten--;
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
            CloseWriterIfNotNull();
            CloseReaderIfNotNull();
        }



        protected void CloseWriterIfNotNull()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
                Log("CopyMgr: _writer closed");
            }
        }

        protected void CloseReaderIfNotNull()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
                Log("CopyMgr: _reader closed");
            }
        }


        protected void Log(string logLine)
        {
            MauiProgram.Log.LogLine(logLine);
        }


        public void LogErrMsgsIfAny(string headerLine)
        {
            if (ErrMsgs.Count == 0)
                return;

            MauiProgram.Log.LogLine(headerLine, false);
            for (int i = 0; i < ErrMsgs.Count; i++)
            {
                MauiProgram.Log.LogLine($"{i+1}: {ErrMsgs[i]}");
            }
        }



        public static T[] AddOneToArray<T>(IEnumerable<T> source, T add)
        {
            var retList = source.ToList();
            retList.Add(add);
            return retList.ToArray();
        }


        public int CalcTotalNumFilesToCopy(string[] subParts,
                    string[] folderNamesToCopy, string[] fileNamesToCopy)
        {
            int totalNum = fileNamesToCopy.Length;
            foreach (string folderName in folderNamesToCopy)
            {
                totalNum += _fileDataService.CalcTotalNumFilesOfFolderWithSubfolders(
                        _settings.FullPathRoot, _settings.AndroidUriRoot,
                        subParts, folderName);
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
            public string[] ClientSubPartsOrNull { get; protected set; }
            public int IdxStartClientpathInSvrSubParts { get; protected set; }
            public string[] FileNames { get; protected set; }
            public string[] FolderNames { get; protected set; }
            public int CurrIdxFiles { get; set; } = 0;
            public int CurrIdxFolders { get; set; } = 0;


            //JEEWEE
            //public string PathOnSvrWindows { get => _settings.PathFromRootAndSubPartsWindows(
            //            SvrSubParts);
            //}

            public string JoinedSvrSubPartsVisibleToClient { get => String.Join("/", SvrSubParts,
                    IdxStartClientpathInSvrSubParts,
                    SvrSubParts.Length - IdxStartClientpathInSvrSubParts); }

            public string JoinedSubPartsOnSvr
            {
                get => String.Join("/", SvrSubParts);
            }

            //JEEWEE SEEMS UNUSED
            //public string[] SvrSubPartsVisibleToClient
            //{
            //    get
            //    {
            //        int len = SvrSubParts.Length - IdxStartClientpathInSvrSubParts;
            //        var retArr = new string[len];
            //        Array.Copy(SvrSubParts, IdxStartClientpathInSvrSubParts, retArr, 0, len);
            //        return retArr;
            //    }
            //}




            /// <summary>
            /// Ctor for first NavLevel to be created, at level that was selected by client
            /// </summary>
            /// <param name="fileDataService"></param>
            /// <param name="settings"></param>
            /// <param name="svrSubParts">on client selected server path; client top path starts here; also if TO server</param>
            /// <param name="clientSubPartsOrNull">null if we are on server</param>
            /// <param name="folderNamesSelection">on client selected folders to copy</param>
            /// <param name="fileNamesSelection">on client selected files to copy</param>
            public NavLevel(FileDataService fileDataService, Settings settings,
                    string[] svrSubParts, string[] clientSubPartsOrNull,
                    string[] folderNamesSelection, string[] fileNamesSelection)
            {
                _fileDataService = fileDataService;
                _settings = settings;
                SvrSubParts = svrSubParts;
                ClientSubPartsOrNull = clientSubPartsOrNull;        
                IdxStartClientpathInSvrSubParts = svrSubParts.Length;

                FolderNames = folderNamesSelection;
                FileNames = fileNamesSelection;
            }


            /// <summary>
            /// Ctor to create the NavLevel for a folder on a below level
            /// </summary>
            /// <param name="navLevelParent"></param>
            /// <param name="folderName"></param>
            public NavLevel(NavLevel navLevelParent, string folderName,
                    bool clientToSvr)
            {
                _fileDataService = navLevelParent._fileDataService;
                _settings = navLevelParent._settings;

                if (clientToSvr)
                {
                    ClientSubPartsOrNull = AddOneToArray(navLevelParent.ClientSubPartsOrNull, folderName);
                }
                SvrSubParts = AddOneToArray(navLevelParent.SvrSubParts, folderName);

                IdxStartClientpathInSvrSubParts = navLevelParent.IdxStartClientpathInSvrSubParts;

                string[] srcSubParts = clientToSvr ? ClientSubPartsOrNull : SvrSubParts; 
                FolderNames = _fileDataService.GetDirFolderNamesGeneric(
                            _settings.FullPathRoot,
                            _settings.AndroidUriRoot, srcSubParts);
                FileNames = _fileDataService.GetDirFileNamesGeneric(
                            _settings.FullPathRoot,
                            _settings.AndroidUriRoot, srcSubParts);
            }
        }
    }



    public class CopyCounters
    {
        public int FoldersCreated = 0;
        public int FilesCreated = 0;
        public int FilesOverwritten = 0;
        public int FilesSkipped = 0;
        public int DtProblems = 0;
        public int ErrHashesDiff = 0;

        public CopyCounters()
        { }

        public CopyCounters(int numFoldersCreated, int numFilesCreated,
                int numFilesOverwritten, int numFilesSkipped, int numDtProblems,
                int errHashesDiff)
        {
            FoldersCreated = numFoldersCreated;
            FilesCreated = numFilesCreated;
            FilesOverwritten = numFilesOverwritten;
            FilesSkipped = numFilesSkipped;
            DtProblems = numDtProblems;
            ErrHashesDiff = errHashesDiff;
        }

    }
}
