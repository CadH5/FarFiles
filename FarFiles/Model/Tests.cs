using FarFiles.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public class Tests
    {
        protected int _numPassed = 0;
        protected int _numFailed = 0;
        protected int _numExceptions = 0;
        protected FileDataService _fileDataService;


        public async Task DoTestsWindowsAsync(FileDataService fileDataService)
        {
#if ANDROID
            throw new Exception("DoTestsWindowsAsync: not for Android!");
#endif

            _fileDataService = fileDataService;
            _numPassed = 0;
            _numFailed = 0;
            _numExceptions = 0;

            string fullPathLog = "C:\\temp\\janDebug";
            using (var wrLog = new StreamWriter(fullPathLog))
            {
                DoTestMsgSvrCl(wrLog);
                DoTestCopyMgr(wrLog, @"C:\temp\_FARFILESTEST_FILES_MAY_BE_OVERWRITTEN");
            }

            await Shell.Current.DisplayAlert("Info",
                $"Created: {fullPathLog}\nnumPassed={_numPassed}\nnumFailed={_numFailed}\nnumExceptions={_numExceptions}", "OK");
        }


        protected void DoTestMsgSvrCl(StreamWriter wrLog)
        {
            try
            {
                wrLog.WriteLine("DoTestMsgSvrCl()");
                wrLog.WriteLine("================");

                MsgSvrClBase msgSvrCl;

                // MsgSvrClErrorAnswer
                string errMsg = "Error message";
                msgSvrCl = new MsgSvrClErrorAnswer(errMsg);
                AssertEq(wrLog, MsgSvrClType.ERROR, msgSvrCl.Type,
                        "MsgSvrClErrorAnswer.Type");
                AssertEq(wrLog, errMsg, ((MsgSvrClErrorAnswer)msgSvrCl).GetErrMsg(),
                        "MsgSvrClErrorAnswer errmsg");

                // MsgSvrClStringSend
                string strToSend = "str to send";
                msgSvrCl = new MsgSvrClStringSend(strToSend);
                AssertEq(wrLog, MsgSvrClType.STRING_SEND, msgSvrCl.Type,
                        "MsgSvrClStringSend.Type");
                AssertEq(wrLog, strToSend, ((MsgSvrClStringSend)msgSvrCl).GetString(),
                        "MsgSvrClStringSend string to send");
                msgSvrCl = new MsgSvrClStringSend("");
                AssertEq(wrLog, "", ((MsgSvrClStringSend)msgSvrCl).GetString(),
                        "MsgSvrClStringSend empty string to send");

                // MsgSvrClStringAnswer
                msgSvrCl = new MsgSvrClStringAnswer();
                AssertEq(wrLog, MsgSvrClType.STRING_ANS, msgSvrCl.Type,
                        "MsgSvrClStringAnswer.Type");

                // MsgSvrClPathInfoRequest
                string[] svrPathPartsIn = new string[] { "" };
                string[] svrPathPartsOut;
                msgSvrCl = new MsgSvrClPathInfoRequest(svrPathPartsIn);
                AssertEq(wrLog, MsgSvrClType.PATHINFO_REQ, msgSvrCl.Type,
                        "MsgSvrClPathInfoRequest.Type");
                svrPathPartsOut = ((MsgSvrClPathInfoRequest)msgSvrCl).GetSvrSubParts();
                AssertEq(wrLog, svrPathPartsOut, svrPathPartsIn,
                        "MsgSvrClPathInfoRequest svrPathParts");

                svrPathPartsIn = new string[] { "aaa", "bb" };
                msgSvrCl = new MsgSvrClPathInfoRequest(svrPathPartsIn);
                svrPathPartsOut = ((MsgSvrClPathInfoRequest)msgSvrCl).GetSvrSubParts();
                AssertEq(wrLog, svrPathPartsOut, svrPathPartsIn,
                        "MsgSvrClPathInfoRequest svrPathParts");

                // MsgSvrClPathInfoNextpartRequest
                msgSvrCl = new MsgSvrClPathInfoNextpartRequest();
                AssertEq(wrLog, MsgSvrClType.PATHINFONEXT_REQ, msgSvrCl.Type,
                        "MsgSvrClPathInfoNextpartRequest.Type");

                // MsgSvrClPathInfoAnswer
                int seqNrIn = 0;
                string[] folderNames = new string[] {
                        "fo1", "folder2", "f3", "", "f5" };
                string[] fileNames = new string[] {
                        "fi1", "file2", "f3", "", "f5" };
                long[] filesSizes = new long[] {
                        0, 4000, 5000000, 0, 5 };
                var pathInfoAnswerState = new PathInfoAnswerState(folderNames, fileNames, filesSizes);
                msgSvrCl = new MsgSvrClPathInfoAnswer(seqNrIn, true, pathInfoAnswerState,
                            20000);
                AssertEq(wrLog, MsgSvrClType.PATHINFO_ANS, msgSvrCl.Type,
                        "MsgSvrClPathInfoAnswer.Type");
                ((MsgSvrClPathInfoAnswer)msgSvrCl).GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizes(
                        out int seqNrOut, out bool isSvrWritable, out bool isLastOut,
                        out string[] foldersOut, out string[] filesOut, out long[] filesSizesOut);
                AssertEq(wrLog, seqNrIn, seqNrOut,
                        "MsgSvrClPathInfoAnswer seqNr");
                AssertEq(wrLog, true, isSvrWritable,
                        "MsgSvrClPathInfoAnswer isSvrWritable");
                AssertEq(wrLog, true, isLastOut,
                        "MsgSvrClPathInfoAnswer isLast");
                AssertEq(wrLog, folderNames, foldersOut,
                        "MsgSvrClPathInfoAnswer folderNames");
                AssertEq(wrLog, fileNames, filesOut,
                        "MsgSvrClPathInfoAnswer fileNames");
                AssertEq(wrLog, filesSizes, filesSizesOut,
                        "MsgSvrClPathInfoAnswer filesSizes");

                folderNames = new string[0];
                fileNames = new string[0];
                filesSizes = new long[0];
                pathInfoAnswerState = new PathInfoAnswerState(folderNames, fileNames, filesSizes);
                msgSvrCl = new MsgSvrClPathInfoAnswer(seqNrIn, false, pathInfoAnswerState,
                                    20000);
                ((MsgSvrClPathInfoAnswer)msgSvrCl).GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizes(
                        out seqNrOut, out isSvrWritable, out isLastOut,
                        out foldersOut, out filesOut, out filesSizesOut);
                AssertEq(wrLog, true, isLastOut,
                        "MsgSvrClPathInfoAnswer empty isLast");
                AssertEq(wrLog, folderNames, foldersOut,
                        "MsgSvrClPathInfoAnswer empty folderNames");
                AssertEq(wrLog, fileNames, filesOut,
                        "MsgSvrClPathInfoAnswer empty fileNames");
                AssertEq(wrLog, filesSizes, filesSizesOut,
                        "MsgSvrClPathInfoAnswer empty filesSizes");

                var testNames = new string[1000];
                var testSizes = new long[1000];
                for (int i=0; i < 1000; i++)
                {
                    testNames[i] = $"f{i}";
                    testSizes[i] = i;
                }

                int smallBufSizeMoreOrLess = 2000;
                // do two tests: first only folders, then only files
                for (int i = 0; i < 2; i++)
                {
                    int seqNr = 0;
                    folderNames = 0 == i ? testNames : new string[0];
                    fileNames = 1 == i ? testNames : new string[0];
                    filesSizes = 1 == i ? testSizes : new long[0];

                    pathInfoAnswerState = new PathInfoAnswerState(
                                folderNames, fileNames, filesSizes);
                    int prevCounterNum = 0;
                    do
                    {
                        msgSvrCl = new MsgSvrClPathInfoAnswer(seqNr, false,
                                    pathInfoAnswerState, smallBufSizeMoreOrLess);
                        ((MsgSvrClPathInfoAnswer)msgSvrCl).GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizes(
                                out seqNrOut, out isSvrWritable, out isLastOut,
                                out foldersOut, out filesOut, out filesSizesOut);
                        AssertEq(wrLog, seqNr, seqNrOut,
                                "MsgSvrClPathInfoAnswer big seqNr");
                        AssertEq(wrLog, false, isSvrWritable,
                                "MsgSvrClPathInfoAnswer big isSvrWritable");
                        AssertEq(wrLog, pathInfoAnswerState.EndReached, isLastOut,
                                "MsgSvrClPathInfoAnswer big isLast");

                        int till = prevCounterNum + (0 == i ? foldersOut.Length : filesOut.Length);

                        if (0 == i)
                        {
                            AssertEq(wrLog, 0, filesOut.Length,
                                    "MsgSvrClPathInfoAnswer big zero files");
                            AssertEq(wrLog, 0, filesSizesOut.Length,
                                    "MsgSvrClPathInfoAnswer big zero sizes");
                            // I don't want to log all array members:
                            AssertEq(wrLog, true,
                                AssertEq(wrLog, folderNames, foldersOut, "",
                                false, prevCounterNum, till),
                                "MsgSvrClPathInfoAnswer folderNames" +
                                        $" [{prevCounterNum} to {till}]");

                            prevCounterNum += foldersOut.Length;
                        }
                        else
                        {
                            AssertEq(wrLog, 0, foldersOut.Length,
                                    "MsgSvrClPathInfoAnswer big zero folders");
                            AssertEq(wrLog, true,
                                AssertEq(wrLog, fileNames, filesOut, "",
                                false, prevCounterNum, till),
                                "MsgSvrClPathInfoAnswer fileNames" +
                                        $" [{prevCounterNum} to {till}]");
                            AssertEq(wrLog, true,
                                AssertEq(wrLog, filesSizes, filesSizesOut, "",
                                false, prevCounterNum, till),
                                "MsgSvrClPathInfoAnswer fileSizes" +
                                        $" [{prevCounterNum} to {till}]");
                            AssertEq(wrLog, filesOut.Length, filesSizesOut.Length,
                                "MsgSvrClPathInfoAnswer num files, sizes");

                            prevCounterNum += filesOut.Length;
                        }
                        
                        seqNr++;
                    }
                    while (!pathInfoAnswerState.EndReached);
                    AssertEq(wrLog, 1000, prevCounterNum,
                                "MsgSvrClPathInfoAnswer big endtotal");
                }

                // MsgSvrClPathInfoAndroidBusy
                seqNrIn = 777;
                msgSvrCl = new MsgSvrClPathInfoAndroidBusy(seqNrIn);
                AssertEq(wrLog, MsgSvrClType.PATHINFO_ANDROIDBUSY, msgSvrCl.Type,
                        "MsgSvrClPathInfoAndroidBusy.Type");
                ((MsgSvrClPathInfoAndroidBusy)msgSvrCl).GetSeqnr(out seqNrOut);
                AssertEq(wrLog, seqNrIn, seqNrOut,
                        "MsgSvrClPathInfoAndroidBusy seqNr");

                // MsgSvrClPathInfoAndroidStillBusyInq
                msgSvrCl = new MsgSvrClPathInfoAndroidStillBusyInq();
                AssertEq(wrLog, MsgSvrClType.PATHINFO_ISANDRBUSYINQ, msgSvrCl.Type,
                        "MsgSvrClPathInfoAndroidStillBusyInq.Type");


                // MsgSvrClCopyRequest
                folderNames = new string[] {
                        "fo1", "folder2", "f3", "", "f5" };
                fileNames = new string[] {
                        "fi1", "file2", "f3", "", "f5" };
                filesSizes = new long[] {
                        0, 4000, 5000000, 0, 5 };
                svrPathPartsIn = new string[] { "str1", "", "str2" };
                msgSvrCl = new MsgSvrClCopyRequest(svrPathPartsIn, folderNames, fileNames);
                AssertEq(wrLog, MsgSvrClType.COPY_REQ, msgSvrCl.Type,
                        "MsgSvrClCopyRequest.Type");
                ((MsgSvrClCopyRequest)msgSvrCl).GetSubPartsAndFolderAndFileNames(
                        out svrPathPartsOut, out foldersOut, out filesOut);
                AssertEq(wrLog, svrPathPartsOut, svrPathPartsIn,
                        "MsgSvrClCopyRequest svrPathParts");
                AssertEq(wrLog, folderNames, foldersOut,
                        "MsgSvrClCopyRequest folderNames");
                AssertEq(wrLog, fileNames, filesOut,
                        "MsgSvrClCopyRequest fileNames");

                // MsgSvrClCopyNextpartRequest
                msgSvrCl = new MsgSvrClCopyNextpartRequest();
                AssertEq(wrLog, MsgSvrClType.COPYNEXT_REQ, msgSvrCl.Type,
                        "MsgSvrClCopyNextpartRequest.Type");

                // MsgSvrClCopyAnswer
                seqNrIn = 3;
                bool isLastIn = true;
                byte[] dataIn = { 0, 1, 2, 255, 0 };
                msgSvrCl = new MsgSvrClCopyAnswer(seqNrIn, isLastIn, dataIn);
                AssertEq(wrLog, MsgSvrClType.COPY_ANS, msgSvrCl.Type,
                        "MsgSvrClCopyAnswer.Type");
                ((MsgSvrClCopyAnswer)msgSvrCl).GetSeqnrAndIslastAndData(
                        out seqNrOut, out isLastOut, out byte[] dataOut);
                AssertEq(wrLog, seqNrIn, seqNrOut,
                        "MsgSvrClCopyAnswer seqNr");
                AssertEq(wrLog, isLastIn, isLastOut,
                        "MsgSvrClCopyAnswer isLast");
                AssertEq(wrLog, dataIn.Select(b => (long)b).ToArray(),
                        dataOut.Select(b => (long)b).ToArray(),
                        "MsgSvrClCopyAnswer data");

                // MsgSvrClAbortedInfo
                msgSvrCl = new MsgSvrClAbortedInfo();
                AssertEq(wrLog, MsgSvrClType.ABORTED_INFO, msgSvrCl.Type,
                        "MsgSvrClAbortedInfo.Type");

                // MsgSvrClAbortedConfirmation
                msgSvrCl = new MsgSvrClAbortedConfirmation();
                AssertEq(wrLog, MsgSvrClType.ABORTED_CONFIRM, msgSvrCl.Type,
                        "MsgSvrClAbortedConfirmation.Type");

                // MsgSvrClCopyToSvrPart
                seqNrIn = 3;
                isLastIn = true;
                dataIn = new byte[] { 0, 1, 2, 255, 0 };
                msgSvrCl = new MsgSvrClCopyToSvrPart(seqNrIn, isLastIn, dataIn);
                AssertEq(wrLog, MsgSvrClType.COPY_TOSVRPART, msgSvrCl.Type,
                        "MsgSvrClCopyToSvrPart.Type");
                ((MsgSvrClCopyAnswer)msgSvrCl).GetSeqnrAndIslastAndData(
                        out seqNrOut, out isLastOut, out dataOut);
                AssertEq(wrLog, seqNrIn, seqNrOut,
                        "MsgSvrClCopyToSvrPart seqNr");
                AssertEq(wrLog, isLastIn, isLastOut,
                        "MsgSvrClCopyToSvrPart isLast");
                AssertEq(wrLog, dataIn.Select(b => (long)b).ToArray(),
                        dataOut.Select(b => (long)b).ToArray(),
                        "MsgSvrClCopyToSvrPart data");

                // MsgSvrClCopyToSvrConfirmation
                msgSvrCl = new MsgSvrClCopyToSvrConfirmation(
                            new CopyCounters(1, 2, 3, 4, 5), 6);
                AssertEq(wrLog, MsgSvrClType.COPY_TOSVRCONFIRM, msgSvrCl.Type,
                        "MsgSvrClCopyToSvrConfirmation.Type");
                ((MsgSvrClCopyToSvrConfirmation)msgSvrCl).GetNums(out CopyCounters nums,
                            out int numErrMsgs);
                AssertEq(wrLog, 1, nums.FoldersCreated, "nums.FoldersCreated");
                AssertEq(wrLog, 2, nums.FilesCreated, "nums.FilesCreated");
                AssertEq(wrLog, 3, nums.FilesOverwritten, "nums.FilesOverwritten");
                AssertEq(wrLog, 4, nums.FilesSkipped, "nums.FilesSkipped");
                AssertEq(wrLog, 5, nums.DtProblems, "nums.DtProblems");
                AssertEq(wrLog, 6, numErrMsgs, "numErrMsgs");
            }
            catch (Exception exc)
            {
                LogExc(wrLog, "DoTestMsgSvrCl", exc);
            }

            wrLog.WriteLine();
        }


        protected void DoTestCopyMgr(StreamWriter wrLog, string testPath)
        {
#if ANDROID
#else
            try
            {
                wrLog.WriteLine("DoTestCopyMgr() testPath='" + testPath + "'");
                wrLog.WriteLine("===============");

                // Extra security to avoid disasters on my computer:
                if (!testPath.StartsWith(@"C:\temp\_FARFILES"))
                    throw new Exception("IS IT CORRECT TO DELETE ALL FROM " + testPath);

                // cleanup tree from previous run, if present
                var errMsgs = _fileDataService.DeleteDirPlusSubdirsPlusFilesWindows(testPath);
                AssertEq(wrLog, 0, errMsgs.Length, "Num errMsgs DeleteDirPlusSubdirsPlusFilesWindows");
                LogErrMsgsIfAny(wrLog, "ErrMsgs DeleteDirPlusSubdirsPlusFilesWindows:", errMsgs);

                string testPathSvr = Path.Combine(testPath, "Svr");
                var settingsSvr = new Settings()
                {
                    FullPathRoot = testPathSvr,
                    BufSizeMoreOrLess = 200,
                };

                // Determine clientSvrPathParts, and use them also to set up initial test file tree
                var clientSvrPathParts = new List<string> { "aa", "bb" };
                string topDirOnSvr = settingsSvr.PathFromRootAndSubPartsWindows(
                                    clientSvrPathParts.ToArray());
                Directory.CreateDirectory(topDirOnSvr);
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub1"));
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub1", "sub1sub1"));
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub1", "sub1sub2"));
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub2"));

                // manual determinations:
                var createFilesSvr = new List<string> {
                        @"test_1.txt", @"test_notCopy.txt",
                        @"sub1\test_2.txt", @"sub1\test_3_size0.txt",
                        @"sub1\sub1sub1\test_4.txt", @"sub1\sub1sub2\test_5.txt" };
                var folderNamesToCopy = new string[] { "sub1" };
                var fileNamesToCopy = new string[] { "test_1.txt" };
                var expectedFilesClient = new List<string> {
                        @"test_1.txt",
                        @"sub1\test_2.txt", @"sub1\test_3_size0.txt",
                        @"sub1\sub1sub1\test_4.txt", @"sub1\sub1sub2\test_5.txt" };

                string testPathClient = Path.Combine(testPath, "Client");
                Directory.CreateDirectory(testPathClient);
                var settingsClient = new Settings()
                {
                    FullPathRoot = testPathClient,
                    Idx0isOverwr1isSkip = 0,
                    BufSizeMoreOrLess = 200,
                };

                // create the files (one having size 0):
                foreach (var relPathFile in createFilesSvr)
                {
                    using (var wr = new StreamWriter(Path.Combine(topDirOnSvr, relPathFile)))
                    {
                        if (!relPathFile.Contains("size0"))
                            wr.WriteLine(relPathFile);
                    }
                }

                // Test CalcTotalNumFilesOfFolderWithSubfolders
                AssertEq(wrLog, 4, _fileDataService.CalcTotalNumFilesOfFolderWithSubfolders(
                        topDirOnSvr, null, new string[] { "" }, "sub1"),
                        "CalcTotalNumFilesOfFolderWithSubfolders");

                // Now 3 tests 'copy from server': 1. create, 2. overwrite, 3. skip
                // note: the tests do not include real communication through Internet

                int alterRemainingLimit = 40;
                var cploopDescr = new string[] { "create", "overwrite", "skip" };
                for (int iCploop = 0; iCploop < 3; iCploop++)
                {
                    wrLog.WriteLine();
                    wrLog.WriteLine($"Copyloop {iCploop+1}: {cploopDescr[iCploop]}");

                    settingsClient.Idx0isOverwr1isSkip = iCploop == 2 ? 1 : 0;

                    int prevSeqNr = -1;
                    var copyMgrSvr = new CopyMgr(_fileDataService, settingsSvr,
                            alterRemainingLimit);
                    var copyMgrClient = new CopyMgr(_fileDataService, settingsClient);
                    int totalNumFilesToCopyOnSrc = copyMgrSvr.CalcTotalNumFilesToCopy(
                            clientSvrPathParts.ToArray(), folderNamesToCopy, fileNamesToCopy);
                    copyMgrSvr.StartCopyFromOrToSvrOnSvrOrClient(
                            new MsgSvrClCopyRequest(clientSvrPathParts,
                            folderNamesToCopy, fileNamesToCopy));
                    while (true)
                    {
                        MsgSvrClBase msgSvrCl = copyMgrSvr.GetNextPartCopyansFromSrc(false);
                        AssertEq(wrLog, MsgSvrClType.COPY_ANS, msgSvrCl.Type,
                                "answer from svr");
                        int seqNr = ((MsgSvrClCopyAnswer)msgSvrCl).SeqNr;
                        AssertEq(wrLog, prevSeqNr + 1, seqNr,
                                "seqNr from svr");
                        prevSeqNr++;
                        if (copyMgrClient.CreateOnDestFromNextPart((MsgSvrClCopyAnswer)msgSvrCl))
                            break;
                    }

                    LogErrMsgsIfAny(wrLog, "ErrMsgs copyMgrSvr:", copyMgrSvr.ErrMsgs);
                    LogErrMsgsIfAny(wrLog, "ErrMsgs copyMgrClient:", copyMgrClient.ErrMsgs);

                    // check results for this loop
                    foreach (string expectedRelPathFile in expectedFilesClient)
                    {
                        string fullPathSvr = Path.Combine(topDirOnSvr, expectedRelPathFile);
                        string fullPathClient = Path.Combine(testPathClient, expectedRelPathFile);

                        AssertEqFile(wrLog, fullPathSvr, fullPathClient);
                    }

                    AssertEq(wrLog, 5,
                                copyMgrClient.ClientTotalNumFilesToCopyFromOrTo, "ClientTotalNumFilesToCopyFromOrTo from server");
                    AssertEq(wrLog, 0 == iCploop ? 3 : 0,
                                copyMgrClient.Nums.FoldersCreated, "Nums.FoldersCreated");
                    AssertEq(wrLog, 2 == iCploop ? 0 : 5,
                                copyMgrClient.Nums.FilesCreated, "Nums.FilesCreated");
                    AssertEq(wrLog, 1 == iCploop ? 5 : 0,
                                copyMgrClient.Nums.FilesOverwritten, "Nums.FilesOverwritten");
                    AssertEq(wrLog, 2 == iCploop ? 5 : 0,
                                copyMgrClient.Nums.FilesSkipped, "Nums.FilesSkipped");
                }

                // just a save of the original tree on server; now we are going to
                // copy back from client:
                Directory.Move(Path.Combine(topDirOnSvr, "sub1"),
                        Path.Combine(topDirOnSvr, "sub1SAV"));

                // Now 3 tests 'copy TO server': 1. create, 2. overwrite, 3. skip
                for (int iCploop = 0; iCploop < 3; iCploop++)
                {
                    wrLog.WriteLine();
                    wrLog.WriteLine($"TO server: Copyloop {iCploop + 1}: {cploopDescr[iCploop]}");

                    settingsClient.Idx0isOverwr1isSkip = iCploop == 2 ? 1 : 0;

                    int prevSeqNr = -1;
                    var copyMgrSvr = new CopyMgr(_fileDataService, settingsSvr,
                            alterRemainingLimit);
                    var copyMgrClient = new CopyMgr(_fileDataService, settingsClient);
                    var clientSubParts = new string[0];
                    var reqToClientItself = new MsgSvrClCopyRequest(
                            clientSvrPathParts,
                            folderNamesToCopy, fileNamesToCopy);
                    copyMgrClient.StartCopyFromOrToSvrOnSvrOrClient(reqToClientItself,
                            clientSubParts);

                    while (true)
                    {
                        MsgSvrClBase msgSvrCl = (MsgSvrClCopyToSvrPart)copyMgrClient
                                    .GetNextPartCopyansFromSrc(true);
                        AssertEq(wrLog, MsgSvrClType.COPY_TOSVRPART, msgSvrCl.Type,
                                "copydata part from client");
                        int seqNr = ((MsgSvrClCopyToSvrPart)msgSvrCl).SeqNr;
                        AssertEq(wrLog, prevSeqNr + 1, seqNr,
                                "seqNr from client");
                        prevSeqNr++;
                        if (copyMgrSvr.CreateOnDestFromNextPart((MsgSvrClCopyToSvrPart)msgSvrCl))
                            break;
                    }

                    LogErrMsgsIfAny(wrLog, "ErrMsgs copyMgrClient:", copyMgrClient.ErrMsgs);
                    LogErrMsgsIfAny(wrLog, "ErrMsgs copyMgrSvr:", copyMgrSvr.ErrMsgs);

                    foreach (string expectedRelPathFile in expectedFilesClient)
                    {
                        string fullPathSvr = Path.Combine(topDirOnSvr, expectedRelPathFile);
                        string fullPathClient = Path.Combine(testPathClient, expectedRelPathFile);

                        AssertEqFile(wrLog, fullPathSvr, fullPathClient);
                    }

                    AssertEq(wrLog, 5,
                                copyMgrSvr.ClientTotalNumFilesToCopyFromOrTo, "ClientTotalNumFilesToCopyFromOrTo from server");
                    AssertEq(wrLog, 0 == iCploop ? 3 : 0,
                                copyMgrSvr.Nums.FoldersCreated, "Nums.FoldersCreated");
                    AssertEq(wrLog, 2 == iCploop ? 0 : 5,
                                copyMgrSvr.Nums.FilesCreated, "Nums.FilesCreated");
                    
                    // first loop (create): 1 file overwritten that is not in folder sub1 and was not removed (test_1.txt)
                    AssertEq(wrLog, 0 == iCploop ? 1 : (1 == iCploop ? 5 : 0),
                                copyMgrSvr.Nums.FilesOverwritten, "Nums.FilesOverwritten");
                    AssertEq(wrLog, 2 == iCploop ? 5 : 0,
                                copyMgrSvr.Nums.FoldersCreated, "Nums.FilesSkipped");
                }
            }
            catch (Exception exc)
            {
                LogExc(wrLog, "DoTestCopyMgr", exc);
            }

            wrLog.WriteLine();
#endif
        }


        /// <summary>
        /// Returns: true if equal
        /// </summary>
        /// <param name="wrLogOrNull">if null, then don't log</param>
        /// <param name="oExpected"></param>
        /// <param name="oResult"></param>
        /// <param name="descr"></param>
        /// <param name="logAllArrayMembers">true for all logged, if oExpected and oResult are Arrays</param>
        /// <returns></returns>
        protected bool AssertEq(StreamWriter wrLogOrNull, object oExpected, object oResult,
                        string descr,
                        bool logAllArrayMembers = true,
                        int startIdxExp = 0, int tillIdxExp = -1,
                        int startIdxRes = 0, int tillIdxRes = -1)
        {
            bool eq = true;
            Array arrExpected = null;
            Array arrResult = null;

            if (oExpected is string[])
            {
                arrExpected = (string[])oExpected;
                arrResult = (string[])oResult;
            }
            else if (oExpected is long[])
            {
                arrExpected = (long[])oExpected;
                arrResult = (long[])oResult;
            }

            if (null != arrExpected && null != arrResult)
            {
                tillIdxExp = -1 == tillIdxExp ? arrExpected.Length : tillIdxExp;
                tillIdxRes = -1 == tillIdxRes ? arrResult.Length : tillIdxRes;

                eq = AssertEq(wrLogOrNull, tillIdxExp - startIdxExp, 
                        tillIdxRes - startIdxRes,
                        "   arrayLengths");
                if (eq)
                {
                    StreamWriter wrLogArraymemsOrNull = logAllArrayMembers ?
                                wrLogOrNull : null;
                    for (int i = startIdxExp, j = startIdxRes; i < tillIdxExp; i++, j++)
                    {
                        if (oExpected is string[])
                        {
                            eq = AssertEq(wrLogArraymemsOrNull, ((string[])arrExpected)[i],
                                    ((string[])arrResult)[j], $"   array[{i}],[{j}]");
                        }
                        else if (oExpected is long[])
                        {
                            eq = AssertEq(wrLogArraymemsOrNull, ((long[])arrExpected)[i],
                                    ((long[])arrResult)[j], $"   array[{i}],[{j}]");
                        }
                        else
                        {
                            throw new Exception($"PROGRAMMERS: AssertEq: oExpected is {oExpected.GetType()}");
                        }
                        if (!eq)
                            break;
                    }
                }
            }
            else
            {
                string qu = (oExpected is string ? "'" : "");
                if (oExpected is DateTime)
                {
                    eq = Math.Abs(((DateTime)oExpected - (DateTime)oResult)
                                .TotalSeconds) < 1;
                }
                else
                {
                    eq = oExpected.Equals(oResult);
                }
                wrLogOrNull?.WriteLine((eq ? "   EQ  " : "** DIF ") +
                    $": {qu}{oExpected}{qu}, {qu}{oResult}{qu}; {descr}");
                if (eq)
                    _numPassed++;
                else
                    _numFailed++;
            }

            return eq;
        }


        protected bool AssertEqFile(StreamWriter wrLog, string fullPathSvrFile,
                    string fullPathClientFile)
        {
            bool eq = true;
            if (! File.Exists(fullPathSvrFile))
            {
                wrLog.WriteLine($"Error in test: not found 'server' file: '{fullPathSvrFile}'");
                eq = false;
            }
            if (!File.Exists(fullPathClientFile))
            {
                wrLog.WriteLine($"Not found client file: '{fullPathClientFile}'");
                eq = false;
            }

            if (eq)
            {
                long fileSizeSvr = new FileInfo(fullPathSvrFile).Length;
                long fileSizeClient = new FileInfo(fullPathClientFile).Length;
                eq = AssertEq(wrLog, fileSizeSvr, fileSizeClient, $"file sizes for {fullPathClientFile}");
            }
            if (eq)
            {
                byte[] byArrSvr = File.ReadAllBytes(fullPathSvrFile);
                byte[] byArrClient = File.ReadAllBytes(fullPathClientFile);
                eq = AssertEq(wrLog, true, byArrSvr.SequenceEqual(byArrClient),
                        $"contents for {fullPathClientFile}");
            }
            if (eq)
            {
                eq = AssertEq(wrLog, File.GetAttributes(fullPathSvrFile),
                        File.GetAttributes(fullPathClientFile),
                        $"attrs for {fullPathClientFile}");
            }
            if (eq)
            {
                eq = AssertEq(wrLog, File.GetCreationTime(fullPathSvrFile),
                        File.GetCreationTime(fullPathClientFile),
                        $"creationtime for {fullPathClientFile}");
            }
            if (eq)
            {
                eq = AssertEq(wrLog, File.GetLastWriteTime(fullPathSvrFile),
                        File.GetLastWriteTime(fullPathClientFile),
                        $"lastwritetime for {fullPathClientFile}");
            }

            if (eq)
                _numPassed++;
            else
                _numFailed++;

            return eq;
        }


        protected void LogExc(StreamWriter wrLog, string descrExc, Exception exc)
        {
            wrLog.WriteLine($"** EXCEPTON: {descrExc}: {MauiProgram.ExcMsgWithInnerMsgs(exc)}");
            _numExceptions++;
        }


        protected void LogErrMsgsIfAny(StreamWriter wrLog, string descrMsgs, IEnumerable<string> errMsgs)
        {
            if (errMsgs.Count() > 0)
            {
                wrLog.WriteLine();
                wrLog.WriteLine(descrMsgs);
                int nE = 1;
                foreach (string msg in errMsgs)
                {
                    wrLog.WriteLine($"{nE++}. {msg}");
                }
            }
        }
    }
}
