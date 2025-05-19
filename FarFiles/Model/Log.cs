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

        public Log()
        {
            _fullPathLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    LOGFILENAME);
            _doLogging = File.Exists(_fullPathLog);
        }


        public void LogLine(string str, bool withDateTime = true, bool throwExcIfErr = false)
        {
            if (! _doLogging)
                return;

            try
            {
                using (var wrLog = new StreamWriter(_fullPathLog, true))
                {
                    wrLog.WriteLine(
                        (withDateTime ? DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " "
                        : "") +
                        str);
                }
            }
            catch
            {
                if (throwExcIfErr)
                    throw;
            }
        }
    }
}
