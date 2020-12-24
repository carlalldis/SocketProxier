using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketProxierLib
{
    public static class Logger
    {
        public static Action<string> WriteMessage;

        public static Severity LogLevel { get; set; } = Severity.Verbose;

        public static void LogMessage(Severity s, string component, string msg)
        {
            if (s < LogLevel)
                return;

            var outputMsg = $"{DateTime.Now}\t{s}\t{component}\t{msg}";
            WriteMessage?.Invoke(outputMsg);
        }
    }
    public enum Severity
    {
        Verbose,
        Trace,
        Information,
        Warning,
        Error,
        Critical
    }
}
