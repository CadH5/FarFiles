//JEEWEE
//using CoreFoundation;
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
        public async Task DoTestsAsync()
        {
            _numPassed = 0;
            _numFailed = 0;
            _numExceptions = 0;

            string fullPathLog = "C:\\temp\\janDebug";
            using (var wrLog = new StreamWriter(fullPathLog))
            {
                DoTestMsgSvrCl(wrLog);
            }

            await Shell.Current.DisplayAlert("Info",
                $"Created: {fullPathLog}\nnumPassed={_numPassed}\nnumFailed={_numFailed}\nnumExceptions={_numExceptions}", "OK");
        }


        protected void DoTestMsgSvrCl(StreamWriter wrLog)
        {
            try
            {
                MsgSvrClBase msgSvrCl;

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

				string strTest = "Test String";
                msgSvrCl = new MsgSvrClStringSend(strTest);
                AssertEq(wrLog, MsgSvrClType.STRING_SEND, msgSvrCl.Type,
                        "MsgSvrClStringSend.Type");
                AssertEq(wrLog, strTest, ((MsgSvrClStringSend)msgSvrCl).GetString(),
                        "MsgSvrClStringSend string");

                msgSvrCl = new MsgSvrClStringAnswer();
                AssertEq(wrLog, MsgSvrClType.STRING_ANS, msgSvrCl.Type,
                        "MsgSvrClStringAnswer.Type");
            }
            catch (Exception exc)
            {
                LogExc(wrLog, "DoTestMsgSvrCl", exc);
            }
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
                    for (int i=0; i < arrExpected.Length; i++)
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



        protected void LogExc(StreamWriter wrLog, string descrExc, Exception exc)
        {
            wrLog.WriteLine($"** EXCEPTON: {descrExc}: {MauiProgram.ExcMsgWithInnerMsgs(exc)}");
            _numExceptions++;
        }
    }
}
