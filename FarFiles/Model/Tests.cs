//JEEWEE
//using CoreFoundation;
using FarFiles.Services;
using System;
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


        public async Task DoTestsAsync(FileDataService fileDataService)
        {
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

                // MsgSvrClPathInfoAnswer
                string[] folderNames = new string[] {
                        "fo1", "folder2", "f3", "", "f5" };
                string[] fileNames = new string[] {
                        "fi1", "file2", "f3", "", "f5" };
                long[] filesSizes = new long[] {
                        0, 4000, 5000000, 0, 5 };
                msgSvrCl = new MsgSvrClPathInfoAnswer(folderNames, fileNames, filesSizes);
                AssertEq(wrLog, MsgSvrClType.PATHINFO_ANS, msgSvrCl.Type,
                        "MsgSvrClPathInfoAnswer.Type");
                ((MsgSvrClPathInfoAnswer)msgSvrCl).GetFolderAndFileNamesAndSizes(
                        out string[] foldersOut, out string[] filesOut, out long[] filesSizesOut);
                AssertEq(wrLog, folderNames, foldersOut,
                        "MsgSvrClPathInfoAnswer folderNames");
                AssertEq(wrLog, fileNames, filesOut,
                        "MsgSvrClPathInfoAnswer fileNames");
                AssertEq(wrLog, filesSizes, filesSizesOut,
                        "MsgSvrClPathInfoAnswer filesSizes");

                // MsgSvrClCopyRequest
                svrPathPartsIn = new string[] { "str1", "", "str2" };
                msgSvrCl = new MsgSvrClCopyRequest(svrPathPartsIn, folderNames, fileNames);
                AssertEq(wrLog, MsgSvrClType.COPY_REQ, msgSvrCl.Type,
                        "MsgSvrClCopyRequest.Type");
                ((MsgSvrClCopyRequest)msgSvrCl).GetSvrSubPartsAndFolderAndFileNames(
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
                int seqNrIn = 3;
                bool isLastIn = true;
                byte[] dataIn = { 0, 1, 2, 255, 0 };
                msgSvrCl = new MsgSvrClCopyAnswer(3, isLastIn, dataIn);
                AssertEq(wrLog, MsgSvrClType.COPY_ANS, msgSvrCl.Type,
                        "MsgSvrClCopyAnswer.Type");
                ((MsgSvrClCopyAnswer)msgSvrCl).GetSeqnrAndIslastAndData(
                        out int seqNrOut, out bool isLastOut, out byte[] dataOut);
                AssertEq(wrLog, seqNrIn, seqNrOut,
                        "MsgSvrClCopyAnswer seqNr");
                AssertEq(wrLog, isLastIn, isLastOut,
                        "MsgSvrClCopyAnswer isLast");
                AssertEq(wrLog, dataIn.Select(b => (long)b).ToArray(),
                        dataOut.Select(b => (long)b).ToArray(),
                        "MsgSvrClCopyAnswer data");
            }
            catch (Exception exc)
            {
                LogExc(wrLog, "DoTestMsgSvrCl", exc);
            }

            wrLog.WriteLine();
        }


        protected void DoTestCopyMgr(StreamWriter wrLog, string testPath)
        {
            try
            {
                wrLog.WriteLine("DoTestCopyMgr() testPath='" + testPath + "'");
                wrLog.WriteLine("===============");

                // Extra security to avoid disasters on my computer:
                if (!testPath.StartsWith(@"C:\temp\_FARFILES"))
                    throw new Exception("IS IT CORRECT TO DELETE ALL FROM " + testPath);

                var errMsgs = _fileDataService.DeleteDirPlusSubdirsPlusFiles(testPath);
                AssertEq(wrLog, 0, errMsgs.Length, "Num errMsgs DeleteDirPlusSubdirsPlusFiles");
                LogErrMsgsIfAny(wrLog, "ErrMsgs DeleteDirPlusSubdirsPlusFiles:", errMsgs);

                string testPathSvr = Path.Combine(testPath, "Svr");
                var settingsSvr = new Settings()
                {
                    FullPathRoot = testPathSvr,
                };
                var clientSvrPathParts = new List<string> { "aa", "bb" };
                string topDirOnSvr = settingsSvr.PathFromRootAndSubParts(
                                    clientSvrPathParts.ToArray());
                Directory.CreateDirectory(topDirOnSvr);
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub1"));
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub1", "sub1sub1"));
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub1", "sub1sub2"));
                Directory.CreateDirectory(Path.Combine(topDirOnSvr, "sub2"));
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
                };

                foreach (var relPathFile in createFilesSvr)
                {
                    using (var wr = new StreamWriter(Path.Combine(topDirOnSvr, relPathFile)))
                    {
                        if (!relPathFile.Contains("size0"))
                            wr.WriteLine(relPathFile);
                    }
                }

                // Now the test:
                int alterBufSizeMoreOrLess = 200;
                int alterRemainingLimit = 40;

                int prevSeqNr = -1;
                var copyMgrSvr = new CopyMgr(_fileDataService, settingsSvr,
                        alterBufSizeMoreOrLess, alterRemainingLimit);
                var copyMgrClient = new CopyMgr(_fileDataService, settingsClient);
                copyMgrSvr.StartCopyFromSvr(new MsgSvrClCopyRequest(clientSvrPathParts,
                        folderNamesToCopy, fileNamesToCopy));
                while (true)
                {
                    MsgSvrClBase msgSvrCl = copyMgrSvr.GetNextPartCopyansFromSvr();
                    AssertEq(wrLog, MsgSvrClType.COPY_ANS, msgSvrCl.Type,
                            "answer from svr");
                    int seqNr = ((MsgSvrClCopyAnswer)msgSvrCl).SeqNr;
                    AssertEq(wrLog, prevSeqNr + 1, seqNr,
                            "seqNr from svr");
                    prevSeqNr++;
                    if (copyMgrClient.CreateOnClientFromNextPart((MsgSvrClCopyAnswer)msgSvrCl))
                        break;
                }

                LogErrMsgsIfAny(wrLog, "ErrMsgs copyMgrScr:", copyMgrSvr.ErrMsgs);
                LogErrMsgsIfAny(wrLog, "ErrMsgs copyMgrClient:", copyMgrClient.ErrMsgs);

                foreach (string expectedRelPathFile in expectedFilesClient)
                {
                    string fullPathClient = Path.Combine(testPathClient, expectedRelPathFile);
                    string fullPathSvr = Path.Combine(topDirOnSvr, expectedRelPathFile);

                    AssertEqFile(wrLog, fullPathClient, fullPathSvr);
                }
            }
            catch (Exception exc)
            {
                LogExc(wrLog, "DoTestCopyMgr", exc);
            }

            wrLog.WriteLine();
        }


        /// <summary>
        /// Returns: true if equal
        /// </summary>
        /// <param name="wrLog"></param>
        /// <param name="oExpected"></param>
        /// <param name="oResult"></param>
        /// <param name="descr"></param>
        /// <returns></returns>
        protected bool AssertEq(StreamWriter wrLog, object oExpected, object oResult,
                        string descr)
        {
            bool eq = true;
            if (oExpected is string[])
            {
                var arrExpected = (string[])oExpected;
                var arrResult = (string[])oResult;
                eq = AssertEq(wrLog, arrExpected.Length, arrResult.Length,
                        "   arrayLengths");
                if (eq)
                {
                    for (int i = 0; i < arrExpected.Length; i++)
                    {
                        eq = AssertEq(wrLog, arrExpected[i], arrResult[i], $"   array[{i}]");
                        if (!eq)
                            break;
                    }
                }
            }
            else if (oExpected is long[])
            {
                var arrExpected = (long[])oExpected;
                var arrResult = (long[])oResult;
                eq = AssertEq(wrLog, arrExpected.Length, arrResult.Length,
                        "   arrayLengths");
                if (eq)
                {
                    for (int i = 0; i < arrExpected.Length; i++)
                    {
                        eq = AssertEq(wrLog, arrExpected[i], arrResult[i], $"   array[{i}]");
                        if (!eq)
                            break;
                    }
                }
            }
            else
            {
                string qu = (oExpected is string ? "'" : "");
                eq = oExpected.Equals(oResult);
                wrLog.WriteLine((eq ? "   EQ  " : "** DIF ") +
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
