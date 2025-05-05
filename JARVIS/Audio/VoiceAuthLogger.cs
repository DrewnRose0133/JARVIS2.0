<<<<<<< Updated upstream
﻿// === VoiceAuthLogger.cs ===
using System;
using System.IO;

namespace JARVIS.Logging
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JARVIS.Audio
>>>>>>> Stashed changes
{
    public static class VoiceAuthLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice_auth_log.txt");

        public static void Log(string userId, string source = "wake_word", string permissionLevel = "Unknown")
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] User: {userId}, Permission: {permissionLevel}, Source: {source}";
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
            Console.WriteLine("[VoiceAuthLogger] " + line);
        }

        public static void LogFailure(string reason, string source = "wake_word")
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Voice auth failed (source: {source}) → Reason: {reason}";
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
            Console.WriteLine("[VoiceAuthLogger] " + line);
        }
    }
}
