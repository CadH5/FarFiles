using FarFiles.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public class Log
    {
        public const string LOGFILENAME = "_FarFiles.log";
        protected bool _doLogging;
        protected string _fullPathLog;
        protected List<string> _logLinesAndr = new List<string>();

        public Log()
        {
            _fullPathLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    LOGFILENAME);
            _doLogging = File.Exists(_fullPathLog);

#if ANDROID
            //JWdP oct 2025: Android logging: will write logfile in browsed root folder at the end;
            //for this, you must set _doLogging to true
            //_doLogging = true;
#endif
        }


        /// <summary>
        /// Write logline if we are actually doing logging (if _FarFiles.log in SpecialFolder existed at startup time)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="withDateTime"></param>
        /// <param name="throwExcIfErr"></param>
        public void LogLine(string str, bool withDateTime = true, bool throwExcIfErr = false)
        {

            string strLog = (withDateTime ? DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " "
                    : "") + str;

            if (! _doLogging)
                return;

#if ANDROID
            Console.WriteLine(strLog);
            _logLinesAndr.Add(strLog);
#else
            try
            {
                using (var wrLog = new StreamWriter(_fullPathLog, true))
                {
                    wrLog.WriteLine(strLog);
                }
            }
            catch
            {
                if (throwExcIfErr)
                    throw;
            }
#endif
        }


        public async void WriteLogLinesAndroidAsync(FileDataService fileDataService)
        {
#if ANDROID
            if (_logLinesAndr.Count > 0)
            {
                try
                {
                    using (var writer = fileDataService.OpenBinaryWriterGeneric(
                                MauiProgram.Settings.FullPathRoot, MauiProgram.Settings.AndroidUriRoot,
                                new string[0], LOGFILENAME, false,
                                out bool logFileExistedBefore))
                    {
                        foreach (string line in _logLinesAndr)
                        {
                            writer.Write(Encoding.ASCII.GetBytes(line + Environment.NewLine));
                        }
                    }
                }
                catch (Exception exc)
                {
                    await Shell.Current.DisplayAlert("Error",
                        $"Error writing Android log lines: {exc.Message}", "OK");
                }
            }
#endif
        }


    }
}
