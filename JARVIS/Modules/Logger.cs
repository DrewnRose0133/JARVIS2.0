
using System;

namespace JARVIS.Modules
{
    public static class Logger
    {
        public static void Log(string message)
        {
            string logMessage = $"[{DateTime.Now}] {message}";
            Console.WriteLine(logMessage);
            System.IO.File.AppendAllText("jarvis.log", logMessage + Environment.NewLine);
        }
    }
}
