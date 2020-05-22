using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tedd.VirtualNetworkCam
{
    public static class Logger
    {
        private static FileStream _logFile;
        private static StreamWriter _sw;

        static Logger()
        {
_logFile = File.Open(Path.Combine(Path.GetTempPath(), "Tedd.VirtualNetworkCam.log"), FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.ReadWrite);
_sw = new StreamWriter(_logFile, Encoding.UTF8);
            _sw.AutoFlush = true;
        }

        public static void Debug(string message)
        {
            swWrite(message);
            System.Diagnostics.Debug.WriteLine(message ?? "<null>");
        }      
        public static void Info(string message)
        {
            swWrite(message);
            System.Diagnostics.Debug.WriteLine(message ?? "<null>");
        }

        public static void Error(Exception exception, string message)
        {
            var str = (message ?? "<null>") + "\t" + (exception?.ToString() ?? "<null>");
            swWrite(str);
            System.Diagnostics.Debug.WriteLine(str);
        }

        private static void swWrite(string str)
        {
            lock (_sw)
            {
                _sw?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]" + (str ?? "<null>"));
                _sw?.Flush();
                _logFile?.Flush(true);
            }

        }

    }
}
