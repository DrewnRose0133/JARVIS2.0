// === CommandHistoryManager.cs ===
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace JARVIS.Memory
{
    public static class CommandHistoryManager
    {
        private static readonly ConcurrentDictionary<string, List<string>> _userHistory = new();
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history");

        static CommandHistoryManager()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }

        public static void LogCommand(string userId, string command)
        {
            if (!_userHistory.ContainsKey(userId))
                _userHistory[userId] = new List<string>();

            _userHistory[userId].Add(command);

            var filePath = GetUserLogPath(userId);
            File.AppendAllText(filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {command}{Environment.NewLine}");
        }

        public static List<string> GetRecentCommands(string userId, int count = 5)
        {
            if (_userHistory.TryGetValue(userId, out var list))
                return list.GetRange(Math.Max(0, list.Count - count), Math.Min(count, list.Count));

            return new List<string>();
        }

        public static void ClearHistory(string userId)
        {
            _userHistory[userId] = new List<string>();
            var filePath = GetUserLogPath(userId);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private static string GetUserLogPath(string userId)
        {
            return Path.Combine(LogDirectory, $"{userId.ToLowerInvariant()}_commands.txt");
        }
    }
}
